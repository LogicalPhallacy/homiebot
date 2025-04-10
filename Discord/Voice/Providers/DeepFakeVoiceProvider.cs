using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Models;
using Homiebot.Models;

namespace Homiebot.Discord.Voice.Providers
{
    internal class DeepFakeVoiceStartResponse
    {
        public bool success {get;set;}
        public string inference_job_token {get;set;}
    }
    internal class DeepFakeVoiceProgressResponse
    {
        public bool success {get;set;}
        public DeepFakeVoiceProgress? state {get;set;}
    }
    internal class DeepFakeVoiceProgress
    {
        public string job_token {get;set;}
        public string status {get;set;}
        public string? maybe_extra_status_description {get;set;}
        public int attempt_count {get;set;}
        public string? maybe_result_token {get;set;}
        public string? maybe_public_bucket_wav_audio_path {get;set;}
        public string model_token {get;set;}
        public string tts_model_type {get;set;}
        public string title {get;set;}
        public DateTime created_at {get;set;}
        public DateTime updated_at {get;set;}
        public override string ToString() => $" Status:{status} Token:{job_token} AttemptCount{attempt_count}\n ModelToken:{model_token} TTSModelType:{tts_model_type} Title:{title}\n Created:{created_at} Updated:{updated_at} extra:{maybe_extra_status_description}";
    }
    internal class DeepFakeVoiceRequest
    {
        public string uuid_idempotency_token {get;set;}
        public string tts_model_token {get;set;}
        public string inference_text {get;set;}
    }
    
    public class DeepFakeVoiceProvider : IVoiceProvider
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly DeepFakeVoiceConfig deepFake;
        private readonly string startEndpoint;
        private readonly string progressEndpoint;
        private Dictionary<string, VoicePersona> voices;
        private const int sampleRate = 22050;
        private const int charLimit = 240;
        
        public IEnumerable<VoicePersona> ListVoices => voices.Values;

        private VoicePersona currentVoice;
        
        public string GetAvailableVoiceText() => $"There are too many deepfake voices to list, go to {deepFake.ProviderHomePage} to find some and use the ;;searchvoice command to figure out how to select them";

        public DeepFakeVoiceProvider(ILogger logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            deepFake = configuration.GetSection("DeepFakeVoiceSettings").Get<DeepFakeVoiceConfig>();
            logger.LogInformation("Assembling Deepfake Voice Options");
            startEndpoint = deepFake.ProviderApiRoot + "/inference";
            progressEndpoint = deepFake.ProviderApiRoot + "/job/";
            voices = new();
            foreach(var kvp in deepFake.Voices)
            {
                voices.Add(kvp.Key, new VoicePersona(this, kvp.Key, charLimit, VoiceSex.Unspecified, sampleRate, description: kvp.Value));
            }
        }
        public VoicePersona ActiveVoice { get => currentVoice; set => currentVoice = value; }

        public string Name => "DeepFake Voice Provider";

        private async Task<string> StartDeepFakeVoiceGeneration(TextToSpeak text, CommandContext context)
        {
            using(var client = GetHttpClient())
            {
                var reqData = new DeepFakeVoiceRequest{
                    inference_text = text.Text,
                    tts_model_token = "TM:"+currentVoice.VoiceName,
                    uuid_idempotency_token = Guid.NewGuid().ToString()
                };
                using var content = JsonContent.Create<DeepFakeVoiceRequest>(reqData, mediaType:System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
                
                var resp = await client.PostAsync(startEndpoint, content);
                if(resp.IsSuccessStatusCode)
                {
                    var startInfo = await JsonSerializer.DeserializeAsync<DeepFakeVoiceStartResponse>(await resp.Content.ReadAsStreamAsync());
                    if(startInfo.success)
                        return startInfo.inference_job_token;
                    else
                        await context.RespondAsync("deepfake website says no");
                }else{
                    await context.RespondAsync($"Error when trying to start deepfake generation: {resp.StatusCode}");
                }
                return string.Empty;
            }
        }
        private async Task<string> AwaitDeepFakeGeneration(string id, CommandContext context)
        {
            using var client = GetHttpClient();
            string localprogress = progressEndpoint + id;
            string generatedAudio = string.Empty;
            int checks = 0;
            while(string.IsNullOrEmpty(generatedAudio))
            {
                // first wait our predefined
                await Task.Delay(TimeSpan.FromSeconds(deepFake.RecheckDelay));
                // now increment checks
                checks++;
                // hit the endpoint
                var resp = await client.GetAsync(localprogress);
                DeepFakeVoiceProgressResponse progressInfo = null;
                if(resp.IsSuccessStatusCode)
                {
                    progressInfo = await JsonSerializer.DeserializeAsync<DeepFakeVoiceProgressResponse>(await resp.Content.ReadAsStreamAsync());
                    if(!string.IsNullOrWhiteSpace(progressInfo.state?.maybe_public_bucket_wav_audio_path))
                        generatedAudio = progressInfo.state?.maybe_public_bucket_wav_audio_path;
                }
                if(checks > deepFake.RecheckAttempts)
                {
                    string failstring = $"Checked {checks} times with the deepfake folks and still don't have audio.";
                    if(progressInfo != null)
                    {
                        failstring += $"\n Last Progress from deepfake provider Success: {progressInfo.success}";
                        if(progressInfo.state != null)
                            failstring+= progressInfo.state.ToString();
                    }
                    await context.RespondAsync(failstring);
                    return string.Empty;
                }
            }
            return generatedAudio;
        }

        private async Task StreamAudio(string generatedAudio, VoiceTransmitSink outStream, Func<Task> speaking, Action startSpeaking, Action stopSpeaking, CommandContext context)
        {
            string localEnd = deepFake.ProviderStorageRoot + generatedAudio;
            using var client = GetHttpClient(true);
            await speaking();
            startSpeaking();
            try
            {
                await AudioConversionHelper.ConvertForDiscord(await client.GetByteArrayAsync(localEnd) ,currentVoice.SampleRate,1,outStream,logger);
            }
            catch(Exception e)
            {
                await context.RespondAsync($"Got an error when trying to get the audio file, bummer. {e.Message}");
            }
            finally
            {
                stopSpeaking();
            }
        }

        public async Task SpeakAsync(TextToSpeak text, VoiceTransmitSink outStream, Func<Task> speaking, Action startSpeaking, Action stopSpeaking, CommandContext context)
        {
            var track = await StartDeepFakeVoiceGeneration(text, context);
            if(string.IsNullOrWhiteSpace(track))
                return;
            await context.RespondAsync("Voice generation started, this may take a while");
            var file = await AwaitDeepFakeGeneration(track, context);
            if(string.IsNullOrWhiteSpace(file))
                return;
            await StreamAudio(file, outStream, speaking, startSpeaking, startSpeaking, context);
        }
        private HttpClient GetHttpClient(bool storage = false) 
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("accept",(storage ? "*/*" :"application/json"));
            client.DefaultRequestHeaders.Add("user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4814.0 Safari/537.36 Edg/99.0.1135.5");
            client.DefaultRequestHeaders.Add("origin",deepFake.ProviderHomePage);
            client.DefaultRequestHeaders.Add("sec-fetch-site",(storage ? "cross-site" : "same-site"));
            client.DefaultRequestHeaders.Add("sec-fetch-mode",(storage ? "no-cors" :"cors"));
            client.DefaultRequestHeaders.Add("sec-fetch-dest",(storage ? "audio" :"empty"));
            client.DefaultRequestHeaders.Add("referer",deepFake.ProviderHomePage);
            client.DefaultRequestHeaders.Add("accept-encoding",(storage ? "identity;q=1, *;q=0" :"gzip, deflate, br"));
            client.DefaultRequestHeaders.Add("accept-language","en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("pragma", "no-cache");
            client.DefaultRequestHeaders.Add("cache-control", "no-cache");
            client.DefaultRequestHeaders.Add("sec-ch-ua","\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"99\", \"Microsoft Edge\";v=\"99\"");
            client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");

            return client;
        }
    }
}
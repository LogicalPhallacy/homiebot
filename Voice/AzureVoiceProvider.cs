using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using DSharpPlus.CommandsNext;

namespace homiebot.voice 
{
    class AzureVoiceConfig
    {
        public Uri EndPoint {get;set;}
        public string SubscriptionKey {get;set;}
        public string Region {get;set;}
    }

    class AzureVoice 
    {
        public string Name {get; set;}
        public string ShortName {get; set;}
        public string Gender {get; set;}
        public string Locale {get; set;}
        public string SampleRateHertz {get; set;}
        public string VoiceType {get; set;}
    }
    class AzureVoiceProvider : IVoiceProvider
    {
        private const string language = SSMLConversionHelper.langstring;
        private const string langLocale = SSMLConversionHelper.langcodestring;
        private const int charLimit = 420;
        private readonly ILogger logger;
        private IConfiguration configuration;
        private SpeechConfig speechConfig;
        private AzureVoiceConfig azureVoiceConfig;
        private AzureVoice currentVoice;
        private IEnumerable<AzureVoice> availableVoices;
        private int semitonePitch = 0;
        private VoiceMood mood = VoiceMood.chat;
        private VoiceSpeed speed = VoiceSpeed.Normal;
        private DateTime tokenIssueTime;
        public AzureVoiceProvider(ILogger logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            azureVoiceConfig = configuration.GetSection("AzureVoiceConfig").Get<AzureVoiceConfig>();
            speechConfig = SpeechConfig.FromSubscription(azureVoiceConfig.SubscriptionKey, azureVoiceConfig.Region);
            speechConfig.SetProfanity(ProfanityOption.Raw);
            speechConfig.SetProperty("SpeechServiceResponse_Synthesis_WordBoundaryEnabled", "false");
            tokenIssueTime = DateTime.Now;
            var allVoices = getAvailableVoices().GetAwaiter().GetResult();
            availableVoices = allVoices.Where(v => v.Locale.StartsWith(language));
            currentVoice = availableVoices.FirstOrDefault();
        }
        public IEnumerable<VoicePersona> ListVoices => listVoices();

        private void reconnectIfNeeded()
        {
            if(DateTime.Now > tokenIssueTime.AddMinutes(9).AddSeconds(30))
            {
                speechConfig = SpeechConfig.FromSubscription(speechConfig.SubscriptionKey,speechConfig.Region);
            }
        }

        private VoicePersona convertAzureVoice(AzureVoice azv)
        {
            var voice = new VoicePersona(this,azv.ShortName,charLimit,Enum.Parse<VoiceSex>(azv.Gender),int.Parse(azv.SampleRateHertz));
            if(azv.ShortName == "en-US-AriaNeural"){
                voice.Mood = mood;
            }
            voice.SemitoneAdjust = semitonePitch;
            voice.Speed = speed;
            return voice;
        }

        private IEnumerable<VoicePersona> listVoices()
        {
            foreach(var voice in availableVoices)
            {
                yield return convertAzureVoice(voice);
            }
        }

        private void setCurrentVoice(VoicePersona voice)
        {
            currentVoice = availableVoices.Where(azv => azv.ShortName == voice.VoiceName).FirstOrDefault();
            speed = voice.Speed;
            semitonePitch = voice.SemitoneAdjust;
            if(voice.Mood != VoiceMood.Unavailable)
            {
                mood = voice.Mood;
            }

        }

        private async Task<IEnumerable<AzureVoice>> getAvailableVoices()
        {
            using(var client = new HttpClient(new HttpClientHandler{SslProtocols = System.Security.Authentication.SslProtocols.Tls12}))
            {
                string endpoint = $"https://{azureVoiceConfig.Region}.tts.speech.microsoft.com" + "/cognitiveservices/voices/list";
                reconnectIfNeeded();
                client.DefaultRequestHeaders.Add("Authorization",$"Bearer {await FetchTokenAsync(azureVoiceConfig.EndPoint.ToString(),azureVoiceConfig.SubscriptionKey)}");
                var resp = await client.GetAsync(endpoint);
                if(resp.IsSuccessStatusCode){
                    return await JsonSerializer.DeserializeAsync<IEnumerable<AzureVoice>>(await resp.Content.ReadAsStreamAsync());
                }else{
                    logger.LogError("Bad response from endpoint: {responseCode} : {response}", resp.StatusCode,await resp.Content.ReadAsStringAsync());
                    return null;
                }
            }
        }

        private async Task<string> FetchTokenAsync(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", azureVoiceConfig.SubscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(fetchUri);

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                Console.WriteLine("Token Uri: {0}", uriBuilder.Uri.AbsoluteUri);
                return await result.Content.ReadAsStringAsync();
            }
        }

        public VoicePersona ActiveVoice { get => convertAzureVoice(currentVoice); set => setCurrentVoice(value); }

        public string Name => "Azure Text to Speech";

        public async Task SpeakAsync(TextToSpeak text, Stream outStream, CommandContext context)
        {
            speechConfig.SetSpeechSynthesisOutputFormat( SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);
            speechConfig.SpeechSynthesisVoiceName = currentVoice.ShortName;
            using(var speech = new SpeechSynthesizer(speechConfig,null))
            {
                string ssml = SSMLConversionHelper.CreateSSMLString(ActiveVoice,text.Text);
                var result = await speech.SpeakSsmlAsync(ssml);
                if(result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    AudioConversionHelper.ConvertForDiscord(result.AudioData,int.Parse(currentVoice.SampleRateHertz),1,outStream,logger);
                    return;
                }
                var err = SpeechSynthesisCancellationDetails.FromResult(result);
                
                await context.RespondAsync($"Synthesis failed Code: {err.ErrorCode} Details: {err.ErrorDetails} Reason: {err.Reason}");
            }
        }
    }
}
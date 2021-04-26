using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Models;

namespace Homiebot.Discord.Voice.Providers
{
    public class DeepFakeVoiceResponse
    {
        public string audio_base64 {get;set;}
    }
    public class DeepFakeVoiceRequest
    {
        public string text {get;set;}
        public string speaker {get;set;}
    }
    public class DeepFakeVoiceProvider : IVoiceProvider
    {
        private readonly ILogger logger;
        private const int sampleRate = 22050;
        private const int charLimit = 240;
        private const string endpoint = "https://mumble.stream/speak_spectrogram"; 
        private static string[] voices = new string[]{
            "alan-rickman",
"anderson-cooper",
"arnold-schwarzenegger",
"bart-simpson",
"ben-stein",
"betty-white",
"bill-gates",
"bill-nye",
"bryan-cranston",
"christopher-lee",
"craig-ferguson",
"danny-devito",
"david-cross",
"dr-phil-mcgraw",
"george-takei",
"gilbert-gottfried",
"hillary-clinton",
"homer-simpson",
"j-k-simmons",
"james-earl-jones",
"john-oliver",
"judi-dench",
"larry-king",
"leonard-nimoy",
"lisa-simpson",
"mark-zuckerberg",
"michael-rosen",
"fred-rogers",
"mr-krabs",
"neil-degrasse-tyson",
"palmer-luckey",
"paul-graham",
"peter-thiel",
"richard-nixon",
"jimmy-carter",
"ronald-reagan",
"bill-clinton",
"george-w-bush",
"barack-obama",
"sam-altman",
"sarah-palin",
"shohreh-aghdashloo",
"david-attenborough",
"spongebob-squarepants",
"squidward",
"tucker-carlson",
"tupac-shakur",
"wilford-brimley",
"wizard"
        };
        public IEnumerable<VoicePersona> ListVoices => generatePersonas();

        private VoicePersona currentVoice;

        public DeepFakeVoiceProvider(ILogger logger)
        {
            this.logger = logger;
        }
        private IEnumerable<VoicePersona> generatePersonas() {
            foreach(var voice in voices)
            {
                yield return new VoicePersona(this,voice,charLimit,VoiceSex.Unspecified,sampleRate);
            }
        }
        public VoicePersona ActiveVoice { get => currentVoice; set => currentVoice = value; }

        public string Name => "DeepFake Voice Provider";

        public async Task SpeakAsync(TextToSpeak text, VoiceTransmitSink outStream, CommandContext context)
        {
            var reqdata =new DeepFakeVoiceRequest{
                speaker = currentVoice.VoiceName,
                text = text.Text
                };
            using(var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept","application/json");
                client.DefaultRequestHeaders.Add("User-Agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36 Edg/84.0.522.52");
                client.DefaultRequestHeaders.Add("Origin","https://vo.codes");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site","cross-site");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode","cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest","empty");
                client.DefaultRequestHeaders.Add("Referer","https://vo.codes/");
                client.DefaultRequestHeaders.Add("Accept-Encoding","gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Accept-Language","en-US,en;q=0.9");
                await context.RespondAsync("Generating voice, this may take a while");
                var resp = await client.PostAsync(endpoint,JsonContent.Create<DeepFakeVoiceRequest>(reqdata));
                if(resp.IsSuccessStatusCode){
                    var response = JsonSerializer.Deserialize<DeepFakeVoiceResponse>(await resp.Content.ReadAsStringAsync());
                    await AudioConversionHelper.ConvertForDiscord(System.Convert.FromBase64String(response.audio_base64),currentVoice.SampleRate,1,outStream,logger);
                }else{
                    await context.RespondAsync($"Bad response from endpoint: {resp.StatusCode}");
                    return;
                }
            }
        }
    }
}
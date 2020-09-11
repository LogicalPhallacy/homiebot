using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Logging;
using System.IO;
using System;

namespace homiebot.voice 
{
    class GoogleCloudVoiceProvider : IVoiceProvider
    {
        // hardcoding this to english for now
        private const string langstring = "en";
        private const string langcodestring = "en-US";
        private const int charLimit = 420;

        private const int sampleRate = 48000;
        private readonly ILogger logger;
        private TextToSpeechClient internalClient;
        private Voice activeVoice;
        private IEnumerable<Voice> voiceChoices;
        public GoogleCloudVoiceProvider(ILogger logger)
        {
            this.logger = logger;
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS","googlekey.json");
            internalClient = TextToSpeechClient.Create();
            voiceChoices = GetVoices();
            activeVoice = voiceChoices.FirstOrDefault();
        }
        public string Name => "Google Cloud Text to Speech";
        private async Task<IEnumerable<Voice>> GetVoicesAsync()
        {
            var resp = await internalClient.ListVoicesAsync(langstring);
            return resp.Voices;
        }

        private IEnumerable<Voice> GetVoices()
        {
            return internalClient.ListVoices(langstring).Voices;
        }

        public IEnumerable<VoicePersona> ListVoices => voiceChoices.Select(v => convertVoicetoPersona(v));

        VoicePersona IVoiceProvider.ActiveVoice { 
            get => convertVoicetoPersona(activeVoice); 
            set => activeVoice = voiceChoices.Where(v => v.Name == value.VoiceName).FirstOrDefault(); 
            }

        public async Task SpeakAsync(TextToSpeak text, Stream outStream)
        {
            try{
                var synth = await internalClient.SynthesizeSpeechAsync(
                    new SynthesisInput{
                        Text = text.Text
                    },
                    new VoiceSelectionParams{
                        Name = activeVoice.Name,
                        LanguageCode = langstring
                    },
                    new AudioConfig{
                        AudioEncoding = AudioEncoding.Linear16,
                        SampleRateHertz = activeVoice.NaturalSampleRateHertz
                    }
                );
                    AudioConversionHelper.ConvertForDiscord(synth.AudioContent.ToArray(),activeVoice.NaturalSampleRateHertz,1,outStream,this.logger);
            }catch(Exception e)
            {
                logger.LogError(e,"Problem with Google Cloud API");
            }
            
        }

        private VoicePersona convertVoicetoPersona(Voice voice){
            return new VoicePersona(this,voice.Name,charLimit,(VoiceSex)voice.SsmlGender,voice.NaturalSampleRateHertz);
        }
    }
}
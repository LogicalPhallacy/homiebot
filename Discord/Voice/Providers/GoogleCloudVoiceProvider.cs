using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GoogleVoice = Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Models;

namespace Homiebot.Discord.Voice.Providers 
{
    class GoogleCloudVoiceProvider : IVoiceProvider
    {
        // hardcoding this to english for now
        private string langstring = SSMLConversionHelper.langstring;
        private const string langcodestring = SSMLConversionHelper.langcodestring;
        private const int charLimit = 420;

        private const int sampleRate = 48000;
        private readonly ILogger logger;
        private GoogleVoice.TextToSpeechClient internalClient;
        private GoogleVoice.Voice activeVoice;
        private double voicePitch = 0;
        private double voiceSpeed = 0;
        private IEnumerable<GoogleVoice.Voice> voiceChoices;
        public GoogleCloudVoiceProvider(ILogger logger)
        {
            this.logger = logger;
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Configs","googlekey.json"));
            internalClient = GoogleVoice.TextToSpeechClient.Create();
            voiceChoices = GetVoices();
            activeVoice = voiceChoices.FirstOrDefault();
        }
        public string Name => "Google Cloud Text to Speech";
        private async Task<IEnumerable<GoogleVoice.Voice>> GetVoicesAsync()
        {
            var resp = await internalClient.ListVoicesAsync(langstring);
            return resp.Voices;
        }

        private IEnumerable<GoogleVoice.Voice> GetVoices()
        {
            return internalClient.ListVoices(langstring).Voices;
        }

        public IEnumerable<VoicePersona> ListVoices => voiceChoices.Select(v => convertVoicetoPersona(v));

        VoicePersona IVoiceProvider.ActiveVoice { 
            get => convertVoicetoPersona(activeVoice); 
            set => setActiveVoicePersona(value); 
            }

        public async Task SpeakAsync(TextToSpeak text, VoiceTransmitSink outStream, CommandContext context = null)
        {
            try{
                var synth = await internalClient.SynthesizeSpeechAsync(
                    new GoogleVoice.SynthesisInput{
                        Text = text.Text
                    },
                    new GoogleVoice.VoiceSelectionParams{
                        Name = activeVoice.Name,
                        LanguageCode = langstring
                    },
                    new GoogleVoice.AudioConfig{
                        AudioEncoding = GoogleVoice.AudioEncoding.Linear16,
                        SampleRateHertz = activeVoice.NaturalSampleRateHertz,
                        Pitch = voicePitch,
                        SpeakingRate = voiceSpeed
                    }
                );
                    await AudioConversionHelper.ConvertForDiscord(synth.AudioContent.ToArray(),activeVoice.NaturalSampleRateHertz,1,outStream,this.logger);
            }catch(Exception e)
            {
                logger.LogError(e,"Problem with Google Cloud API");
            }
            
        }

        private VoicePersona convertVoicetoPersona(GoogleVoice.Voice voice){
            var currentVoice = new VoicePersona(this,voice.Name,charLimit,(VoiceSex)voice.SsmlGender,voice.NaturalSampleRateHertz);
            currentVoice.SemitoneAdjust = (int)voicePitch;
            currentVoice.Speed = (VoiceSpeed)(int)voiceSpeed;
            return currentVoice;
        }
        private void setActiveVoicePersona(VoicePersona vp)
        {
            activeVoice = voiceChoices.Where(v => v.Name == vp.VoiceName).FirstOrDefault();
            voicePitch = vp.SemitoneAdjust;
            voiceSpeed = ((int)vp.Speed)/10;
        }
    }
}
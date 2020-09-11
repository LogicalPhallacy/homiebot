using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace homiebot.voice
{
    public class MultiCloudTTS : ITextToSpeechHelper
    {
        private readonly IServiceProvider services;
        private readonly ILogger logger;

        private List<IVoiceProvider> voiceProviders;
        private VoicePersona currentVoice;
        private List<VoicePersona> voices;
        public MultiCloudTTS(IServiceProvider services, ILogger logger)
        {
            this.logger = logger;
            logger.LogInformation("Initializing multicloud provider");
            voiceProviders = new List<IVoiceProvider>();
            logger.LogInformation("Registering Google Cloud Voice Provider");
            AddVoiceProvider(new GoogleCloudVoiceProvider(logger));
            
            logger.LogInformation("Setting up current voice");
            currentVoice = voiceProviders.FirstOrDefault().ListVoices.FirstOrDefault();
            logger.LogInformation("MultiCloud ready with default voice of {voice}",currentVoice.VoiceName);
        }
        public IEnumerable<IVoiceProvider> VoiceProviders => voiceProviders;

        public VoicePersona CurrentVoice { get => currentVoice; set => currentVoice = value; }

        public IEnumerable<VoicePersona> AvailableVoices => voices;

        public void AddVoiceProvider(IVoiceProvider provider)
        {
            if(voiceProviders == null){
                voiceProviders = new List<IVoiceProvider>();    
            }
            if(!voiceProviders.Contains(provider)){
                voiceProviders.Add(provider);
            }
        }

        public async Task Speak(string text, Stream outStream)
        {
            await currentVoice.VoiceProvider.SpeakAsync(new TextToSpeak{Text = text,VoiceSex = currentVoice.VoiceSex},outStream);
        }
    }
}
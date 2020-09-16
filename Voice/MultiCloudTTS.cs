using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using DSharpPlus.CommandsNext;

namespace homiebot.voice
{
    public class MultiCloudTTS : ITextToSpeechHelper
    {
        private readonly IServiceProvider services;
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        private List<IVoiceProvider> voiceProviders;
        private VoicePersona currentVoice;
        public MultiCloudTTS(IServiceProvider services, ILogger<HomieBot> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            logger.LogInformation("Initializing multicloud provider");
            voiceProviders = new List<IVoiceProvider>();
            logger.LogInformation("Registering Google Cloud Voice Provider");
            AddVoiceProvider(new GoogleCloudVoiceProvider(logger));
            logger.LogInformation("Registering Azure Voices");
            AddVoiceProvider(new AzureVoiceProvider(logger,configuration));            
            logger.LogInformation("Registering DeepFake Voices");
            AddVoiceProvider(new DeepFakeVoiceProvider(logger));
            logger.LogInformation("Setting up current voice");
            currentVoice = voiceProviders.FirstOrDefault().ListVoices.FirstOrDefault();
            logger.LogInformation("MultiCloud ready with default voice of {voice}",currentVoice.VoiceName);
        }
        public IEnumerable<IVoiceProvider> VoiceProviders => voiceProviders;

        public VoicePersona CurrentVoice { 
            get => currentVoice; 
            set => setVoice(value); 
            }
        private void setVoice(VoicePersona newVoice)
        {
            currentVoice = newVoice;
            currentVoice.VoiceProvider.ActiveVoice = newVoice;
        }
        public IEnumerable<VoicePersona> AvailableVoices => getVoices();

        private IEnumerable<VoicePersona> getVoices() 
        {
            foreach(var p in voiceProviders)
            { 
                foreach(var v in p.ListVoices)
                {
                    yield return v;
                }
            }
        }

        public void AddVoiceProvider(IVoiceProvider provider)
        {
            if(voiceProviders == null){
                voiceProviders = new List<IVoiceProvider>();    
            }
            if(!voiceProviders.Contains(provider)){
                voiceProviders.Add(provider);
            }
        }

        public async Task Speak(string text, Stream outStream, CommandContext context = null)
        {
            await currentVoice.VoiceProvider.SpeakAsync(new TextToSpeak{Text = text,VoiceSex = currentVoice.VoiceSex},outStream, context);
        }
    }
}
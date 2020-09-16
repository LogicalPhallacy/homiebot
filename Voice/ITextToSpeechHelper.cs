using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.IO;

namespace homiebot.voice
{
    public interface ITextToSpeechHelper
    {
        public IEnumerable<IVoiceProvider> VoiceProviders {get;}
        public void AddVoiceProvider(IVoiceProvider provider);
        public Task Speak(string text, Stream outstream, CommandContext context = null);
        public VoicePersona CurrentVoice {get; set;}
        public IEnumerable<VoicePersona> AvailableVoices {get;}
    }
}
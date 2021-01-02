using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.IO;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Providers;
using Homiebot.Discord.Voice.Models;

namespace Homiebot.Discord.Voice
{
    public interface ITextToSpeechHelper
    {
        public IEnumerable<IVoiceProvider> VoiceProviders {get;}
        public void AddVoiceProvider(IVoiceProvider provider);
        public Task Speak(string text, VoiceTransmitSink outstream, CommandContext context = null);
        public VoicePersona CurrentVoice {get; set;}
        public IEnumerable<VoicePersona> AvailableVoices {get;}
    }
}
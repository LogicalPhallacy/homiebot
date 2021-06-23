using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Models;
using Homiebot.Discord.Voice.Providers;

namespace Homiebot.Discord.Voice
{
    public class DummyTextToSpeechHelper : ITextToSpeechHelper
    {
        public IEnumerable<IVoiceProvider> VoiceProviders => throw new System.NotImplementedException();

        public VoicePersona CurrentVoice { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public IEnumerable<VoicePersona> AvailableVoices => throw new System.NotImplementedException();

        public void AddVoiceProvider(IVoiceProvider provider)
        {
            throw new System.NotImplementedException();
        }

        public Task Speak(string text, VoiceTransmitSink outstream, CommandContext context = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
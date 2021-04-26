using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.IO;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Models;

namespace Homiebot.Discord.Voice.Providers
{
    public interface IVoiceProvider
    {
        Task SpeakAsync(TextToSpeak text, VoiceTransmitSink outStream, CommandContext context = null);
        IEnumerable<VoicePersona> ListVoices {get;}
        VoicePersona ActiveVoice {get;set;}
        string Name{get;}
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.IO;

namespace homiebot.voice
{
    public interface IVoiceProvider
    {
        Task SpeakAsync(TextToSpeak text, Stream outStream, CommandContext context = null);
        IEnumerable<VoicePersona> ListVoices {get;}
        VoicePersona ActiveVoice {get;set;}
        string Name{get;}
    }
}
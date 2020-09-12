using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace homiebot.voice
{
    public interface IVoiceProvider
    {
        Task SpeakAsync(TextToSpeak text, Stream outStream);
        IEnumerable<VoicePersona> ListVoices {get;}
        VoicePersona ActiveVoice {get;set;}
        string Name{get;}
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using System.IO;
using DSharpPlus.VoiceNext;
using Homiebot.Discord.Voice.Models;
using System;

namespace Homiebot.Discord.Voice.Providers
{
    public interface IVoiceProvider
    {
        Task SpeakAsync(TextToSpeak text, VoiceTransmitSink outStream, Func<Task> speaking, Action startSpeaking, Action stopSpeaking, CommandContext context = null);
        IEnumerable<VoicePersona> ListVoices {get;}
        IEnumerable<VoicePersona> SearchVoices(string searchString) => ListVoices.Where(v => v.SearchName.ToLowerInvariant().Contains(searchString.ToLowerInvariant()));
        VoicePersona ActiveVoice {get;set;}
        string Name{get;}
        string GetAvailableVoiceText()
        {
            string others = $"AVAILABLE VOICES in {Name}:\n";
            others+= string.Join('\n', ListVoices.Select(v => v.DisplayName));
            return others;
        }
    }
}
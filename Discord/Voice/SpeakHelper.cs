using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;

namespace Homiebot.Discord.Voice
{
    public static class SpeechHelper 
    {
        public static async Task Speak(ITextToSpeechHelper textToSpeechHelper, CommandContext context, string text, bool overrideLimit = false)
        {
            var voiceNext = context.Client.GetVoiceNext();
            if(voiceNext == null)
            {
                await context.RespondAsync("Voice isn't enabled at the moment");
                return;
            }
            var connection = voiceNext.GetConnection(context.Guild);
            if(connection == null)
            {
                await context.TriggerTypingAsync();
                await context.RespondAsync("I'm not connected to a channel right now. Tell me to ::getin one");
                return;
            }
            if(text.Length > textToSpeechHelper.CurrentVoice.CharLimit && !overrideLimit)
            {
                await context.TriggerTypingAsync();
                await context.RespondAsync($"Sorry, there's a TTS character limit and you're over it by {textToSpeechHelper.CurrentVoice.CharLimit - text.Length}");
                return;
            }
            await connection.SendSpeakingAsync();
            using(var transit = connection.GetTransmitSink())
            {
                await textToSpeechHelper.Speak(text,transit,context);
                await connection.WaitForPlaybackFinishAsync();
                await transit.FlushAsync();
            }
        }

    }
}
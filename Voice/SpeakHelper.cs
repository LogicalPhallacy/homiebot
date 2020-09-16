using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;

namespace homiebot.voice
{
    public static class SpeechHelper 
    {
        public static async Task Speak(ITextToSpeechHelper textToSpeechHelper, CommandContext context, string text)
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
            if(text.Length > textToSpeechHelper.CurrentVoice.CharLimit)
            {
                await context.TriggerTypingAsync();
                await context.RespondAsync($"Sorry, there's a TTS character limit and you're over it by {textToSpeechHelper.CurrentVoice.CharLimit - text.Length}");
                return;
            }
            await connection.SendSpeakingAsync();
            var transit = connection.GetTransmitStream();
            await textToSpeechHelper.Speak(text,transit,context);
            await connection.WaitForPlaybackFinishAsync();
            await transit.FlushAsync();
            await transit.DisposeAsync();
        }

    }
}
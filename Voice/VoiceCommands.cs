using DSharpPlus.VoiceNext;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace homiebot.voice
{
    public class VoiceCommands : BaseCommandModule
    {
        private readonly ILogger logger;
        private readonly ITextToSpeechHelper textToSpeechHelper;
        public VoiceCommands(ILogger<HomieBot> logger, ITextToSpeechHelper textToSpeechHelper)
        {
            this.logger = logger;
            this.textToSpeechHelper = textToSpeechHelper;
        }

        [Command("getin")]
        [Description("Asks homiebot to join the voice channel the inviting user is currently in")]
        public async Task GetIn(CommandContext context)
        {
            await context.TriggerTypingAsync();
            var voiceConnection = context.Client.GetVoiceNext().GetConnection(context.Guild);
            var channel = context.Member?.VoiceState?.Channel;
            if(channel == null)
            {
                await context.RespondAsync("Sorry homie, you're either in a channel I can't see, or not in a voice channel");
                return;
            }
            if(voiceConnection != null)
            {
                if(voiceConnection.Channel == channel)
                {
                    await context.RespondAsync("I'm already in here with you homie.");
                    return;
                }else
                {
                    await context.RespondAsync($"I'm already in {voiceConnection.Channel.Name}, have me ::getout first");
                    return;
                }
            }
            voiceConnection = await channel.ConnectAsync();
            logger.LogInformation("Registering helper events to leave when idle");
            voiceConnection.UserLeft += (
                async (vcontext) => {
                    if(voiceConnection.Channel.Users.Count() == 1){
                        await context.Channel.SendMessageAsync("I don't like being all alone in VC, so I'm out.");
                        voiceConnection.Disconnect();
                        voiceConnection.Dispose();
                    }
                }
            );
        }

        [Command("getout")]
        [Description("Tells homiebot to leave the voice channel he's connected to")]
        public async Task GetOut(CommandContext context)
        {
            await context.TriggerTypingAsync();
            var voiceConnection = context.Client.GetVoiceNext().GetConnection(context.Guild);
            if(voiceConnection == null)
            {
                await context.RespondAsync("I'm not connected here homie.");
                return;
            }
            voiceConnection.Disconnect();
        }

        [Command("speak")]
        [Description("If connected to a voice channel, Homiebot will use TTS to speak what you asked for")]
        public async Task Speak(CommandContext context, [RemainingText]string text)
        {
            await SpeechHelper.Speak(textToSpeechHelper,context,text);
        }

        [Command("showvoice")]
        [Description("Shows the available TTS voices, add a specific voice to see detailed info on it")]
        public async Task ShowVoice(CommandContext context, [RemainingText] string? text)
        {
            await context.TriggerTypingAsync();
            if(!string.IsNullOrWhiteSpace(text))
            {
                var requestedVoice = textToSpeechHelper.AvailableVoices.Where(v => v.Equals(text)).FirstOrDefault();
                if(requestedVoice == null)
                {
                    await context.RespondAsync("Couldn't find a voice by that name");
                    return;
                }
                await context.RespondAsync($"Voice Information for {requestedVoice.VoiceName}:\n{requestedVoice.ToString()}");
                return;
            }
            await context.RespondAsync($"CURRENT VOICE\n{textToSpeechHelper.CurrentVoice.VoiceName}");
            string others = "AVAILABLE VOICES:\n";
            others+= string.Join(' ',textToSpeechHelper.AvailableVoices.Select(v => v.VoiceName));
            await context.RespondAsync(others);
        }
        [Command("setvoice")]
        [Description("Use with an output selected from ::showvoice to change the TTS voice")]
        public async Task SetVoice(CommandContext context, string text)
        {
            await context.TriggerTypingAsync();
            if(textToSpeechHelper.CurrentVoice.Equals(text)){
                await context.RespondAsync("That's already the current voice");
                return;
            }
            var newVoice = textToSpeechHelper.AvailableVoices.Where(v => v.Equals(text)).FirstOrDefault();
            if(newVoice == null){
                await context.RespondAsync("Sorry can't find the voice you're asking for");
                return;
            }
            textToSpeechHelper.CurrentVoice = newVoice;
            await context.RespondAsync($"Voice updated to:\n{newVoice.ToString()}");
        }
    }
}
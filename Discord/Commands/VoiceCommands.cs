using DSharpPlus.VoiceNext;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;
using Homiebot.Discord.Voice;
using Homiebot.Discord.Voice.Models;
using DSharpPlus.Entities;

namespace Homiebot.Discord.Commands
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
            //var channel = context.Member?.VoiceState?.Channel;
            var channel = context.Guild.Channels.Where( kvp => kvp.Value.Type == ChannelType.Voice).Select(kvp => kvp.Value).FirstOrDefault();
            
            if(channel == null)
            {
                await context.RespondAsync("Sorry homie, you're either in a channel I can't see, or not in a voice channel");
                return;
            }
            if(voiceConnection != null)
            {
                if(voiceConnection.TargetChannel == channel)
                {
                    await context.RespondAsync("I'm already in here with you homie.");
                    return;
                }else
                {
                    await context.RespondAsync($"I'm already in {voiceConnection.TargetChannel.Name}, have me ::getout first");
                    return;
                }
            }
            voiceConnection = await channel.ConnectAsync();
            logger.LogInformation("Registering helper events to leave when idle");
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
        [Aliases("say")]
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
        [Command("setvoicespeed")]
        [Description("Sets a speed for current voice to speak at, valid values are: Normal, xslow, slow, fast, xfast")]
        public async Task SetVoiceSpeed(CommandContext context, string speed)
        {
            await context.TriggerTypingAsync();
            VoiceSpeed v = VoiceSpeed.Normal;
            try
            {
                v = Enum.Parse<VoiceSpeed>(speed);
                var newVoice = textToSpeechHelper.CurrentVoice;
                newVoice.Speed = v;
                textToSpeechHelper.CurrentVoice = newVoice;
                await context.RespondAsync($"Voice speed changed. CurrentVoice is now {textToSpeechHelper.CurrentVoice.ToString()}");
            }
            catch(Exception e)
            {
                await context.RespondAsync("Couldn't parse your response into a valid voice speed, sorry. Voice Speed will not be changed");
            }
        }
        [Command("setvoicepitch")]
        [Description("Adjusts the pitch of the current voice, valid values are: -20 through 20 in whole numbers")]
        public async Task SetVoicePitch(CommandContext context, int pitch)
        {
            await context.TriggerTypingAsync();
            if(pitch < -20 || pitch > 20)
            {
                await context.RespondAsync("Pitch adjustments must be within -20 to 20 semitones, whole numbers only");
                return;
            }
            var newVoice = textToSpeechHelper.CurrentVoice;
            newVoice.SemitoneAdjust = pitch;
            textToSpeechHelper.CurrentVoice = newVoice;
            await context.RespondAsync($"Voice pitch changed. CurrentVoice is now {textToSpeechHelper.CurrentVoice.ToString()}");
        }
        [Command("setvoicemood")]
        [Description("Sets a mood for current voice (if supported), valid values are: newscastformal, newscastcasual, customerservice, chat, cheerful, empathetic")]
        public async Task SetVoiceMood(CommandContext context, string mood)
        {
            await context.TriggerTypingAsync();
            if(textToSpeechHelper.CurrentVoice.Mood == VoiceMood.Unavailable){
                await context.RespondAsync("Cannot change the mood on this voice, sorry");
                return;
            }
            VoiceMood v = VoiceMood.Unavailable;
            try
            {
                v = Enum.Parse<VoiceMood>(mood);
                var newVoice = textToSpeechHelper.CurrentVoice;
                newVoice.Mood = v;
                textToSpeechHelper.CurrentVoice = newVoice;
                await context.RespondAsync($"Voice mood changed. CurrentVoice is now {textToSpeechHelper.CurrentVoice.ToString()}");
            }
            catch(Exception e)
            {
                await context.RespondAsync("Couldn't parse your response into a valid voice speed, sorry. Voice Speed will not be changed");
            }
        }
        
    }
}
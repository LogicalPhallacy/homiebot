using System.Threading.Tasks;
using DSharpPlus.Entities;
using Homiebot.Models;

namespace Homiebot.Discord;
public static class GimmickExtensions
{
    public static async Task RunRESTGimmick (this RESTGimmick rest, DiscordMessage discordMessage)
    {
        if(rest.IsBusy){
            await discordMessage.RespondAsync("I'm already trying to do that homie");
            return;
        }
        await discordMessage.Channel.TriggerTypingAsync();
        await discordMessage.RespondAsync(rest.FormatPreMessage);
        if (await rest.RunRequest()){
            await discordMessage.RespondAsync(rest.GetPostMessage);
        }else{
            await discordMessage.RespondAsync(rest.GetFailMessage);
        }
        rest.FinishGimmick();
    }
}
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using Homiebot.Models;

namespace Homiebot.Discord;
public static class GimmickExtensions
{
    public static async Task RunRESTGimmick (this RESTGimmick rest, CommandContext ctx)
    {
        if(rest.IsBusy){
            await ctx.RespondAsync("I'm already trying to do that homie");
        }
        await ctx.TriggerTypingAsync();
        await ctx.RespondAsync(rest.FormatPreMessage);
        if (await rest.RunRequest()){
            await ctx.RespondAsync(rest.GetPostMessage);
        }else{
            await ctx.RespondAsync(rest.GetFailMessage);
        }
    }
}
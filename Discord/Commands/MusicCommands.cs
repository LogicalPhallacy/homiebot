using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Homiebot.Helpers;
using Homiebot.Models;
using System;
using DSharpPlus.Entities;
using System.Linq;

namespace Homiebot.Discord.Commands;
public static class SongLinkCommandHandler
{
    public static async Task HandleSongLinks(this MessageCreateEventArgs message, DiscordClient sender, ILogger logger)
    {
        if(SongLinkHelper.TestMessageContainsKnownProviderLink(message.Message.Content))
        {
            string songLink = SongLinkHelper.GetLinkFromMessage(message.Message.Content);
            if (string.IsNullOrEmpty(songLink))
                return;
            logger.LogInformation("Found a song link: {link}", songLink);
            try
            {
                var songLinkObject = await SongLinkHelper.GetSongLink(songLink);
                if(songLinkObject.entitiesByUniqueId.Count > 0)
                {
                    await message.Message.RespondAsync(
                        new DiscordEmbedBuilder().WithSongLinkEmbed(songLinkObject).Build()
                    );
                }
            }
            catch(Exception e)
            {
                logger.LogWarning(e, "Couldn't get a valid songlink response");
            }
        }
    }
    public static DiscordEmbedBuilder WithSongLinkEmbed(this DiscordEmbedBuilder builder, SongLink songLink)
    {
        var thumb = songLink.GetBestThumbnail();
        if(thumb != null)
        {
            builder = builder.WithThumbnail(thumb.Item1, thumb.Item2, thumb.Item3);
        }
        return builder.WithUrl(songLink.pageUrl)
            .WithTitle(songLink.FirstTitleEntry)
            .WithDescription(songLink.Description)
            .AddField("Links",
            string.Join(' ', songLink.linksByPlatform.Select(
                link => link.Value.GetEmbedLink(link.Key)
            )));
    }
}
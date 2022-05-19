using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Homiebot.Helpers;
using Homiebot.Models;
using System;
using DSharpPlus.Entities;
using System.Linq;
using System.Collections.Generic;

namespace Homiebot.Discord.Commands;
public static class SongLinkCommandHandler
{
    private static Dictionary<string,bool> fieldassignments = new(){
        {"spotify",true},
        {"itunes",true},
        {"appleMusic",true},
        {"youtube",true},
        {"youtubeMusic",true},
        {"pandora",true},
        {"deezer",true},
        {"tidal",true},
        {"soundcloud",true},
        {"google",false},
        {"googleStore",false},
        {"napster",false},
        {"yandex",false},
        {"spinrilla",false},
        {"audius",false},
        {"audiomack",false},
        {"amazonStore",false},
        {"amazonMusic",false},
    };
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
            .AddField("Links 1",
            string.Join(' ', songLink.linksByPlatform.Where(l => fieldassignments.ContainsKey(l.Key) && fieldassignments[l.Key]).Select(
                link => link.Value.GetEmbedLink(link.Key)
            )?? new string[0]))
            .AddField("Links 2",
            string.Join(' ', songLink.linksByPlatform.Where(l => fieldassignments.ContainsKey(l.Key) && !fieldassignments[l.Key]).Select(
                link => link.Value.GetEmbedLink(link.Key)
            ) ?? new string[0]))
            .AddField("Links 3",
            string.Join(' ', songLink.linksByPlatform.Where(l => !fieldassignments.ContainsKey(l.Key)).Select(
                link => link.Value.GetEmbedLink(link.Key)
            ) ?? new string[0]));
    }
}
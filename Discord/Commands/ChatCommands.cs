using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using Homiebot.Brain;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Http;
using Homiebot.Web;
using System.Diagnostics;

namespace Homiebot.Discord.Commands;

[ModuleLifespan(ModuleLifespan.Transient)]
public class ChatCommands : BaseCommandModule {
    private readonly ILogger<HomieBot> logger;
    private readonly ITextAnalyzer textAnalyzer;
    private Activity? activity = null;
    public override Task BeforeExecutionAsync(CommandContext ctx)
    {
        activity = TelemetryHelpers.StartActivity(ctx.Command.Name);
        return base.BeforeExecutionAsync(ctx);
    }
    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        if(!(activity?.IsStopped ?? true))
        {
            if(activity.Status == ActivityStatusCode.Unset)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            activity?.Stop();
        }
        activity?.Dispose();
        return base.AfterExecutionAsync(ctx);
    }
    public ChatCommands(ILogger<HomieBot> logger, ITextAnalyzer textAnalyzer)
    {
        this.logger = logger;
        this.textAnalyzer = textAnalyzer;
    }

    [Command("tldr")]
    [Description("Asks homiebot to summarize some text for you")]
    public async Task TLDR(CommandContext context, params string[] args){
        await context.TriggerTypingAsync();
        await context.RespondAsync("I'll try, but it might take a minute");
        string url = "";
        if(context.Message.Attachments.Any())
        {
            var attachment = context.Message.Attachments.FirstOrDefault();
            url = attachment.Url;
        }
        else
        {
            // image wasn't attached, so lets hope its in the args
            if (args != null && !string.IsNullOrWhiteSpace(args[0]) && args[0].ToLowerInvariant().StartsWith("http"))
            {
                System.Uri.TryCreate(args[0], UriKind.Absolute, out Uri attachment);
                url = args[0];
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error,"404")?.Stop();
                _ = context.RespondAsync("I couldn't find anything to summarize, sorry");
                return;
            }
        }
        string input = "";
        using var httpClient = new HttpClient();
        input = await httpClient.GetStringAsync(url);
        var embed = await (new DiscordEmbedBuilder().WithBulletPoints(url, textAnalyzer.TLDR(input)));
        await context.RespondAsync(embed.Build());
    }
}

internal static class ChatCommandExtensions {
    public static async Task<DiscordEmbedBuilder> WithBulletPoints(this DiscordEmbedBuilder builder, string originalDoc, IAsyncEnumerable<string> points)
    {
        
        var setup = builder.WithUrl(originalDoc)
            .WithTitle("An Executive Summary by Homiebot");
            var pointNum = 0;
            var description = "";
            await foreach (var point in points){
                description+= $"{pointNum++}. {point}\n";
            }
        return setup.WithDescription(description);
    }
}
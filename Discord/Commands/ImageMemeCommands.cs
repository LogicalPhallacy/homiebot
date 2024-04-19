using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;
using System;
using System.Reflection;
using Homiebot.Models;
using Homiebot.Images;
using System.Net.Http;
using DSharpPlus;
using Google.Api;
using System.IO.Compression;
using Homiebot.Web;

namespace Homiebot.Discord.Commands
{
    [ModuleLifespan(ModuleLifespan.Singleton)]
    class ImageMemeCommands : BaseCommandModule
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        private IEnumerable<MemeTemplate> templates;
        private IEnumerable<ImageCollection> collections;
        private readonly IImageStore imageStore;
        private readonly Random random;
        private readonly IImageProcessor imageProcessor;
        private readonly IEnumerable<string> knownImageEndings = new string[] {
            ".jpg",
            ".jpeg",
            ".gif",
            ".png",
            ".apng",
            ".webm",
            ".mp4",
            ".bmp",
            ".jfif",
            ".jpg_large",
        };
        public delegate Task RunMemeTemplate(CommandContext ctx, params string[] input);
        public delegate Task RunCollection(CommandContext ctx, params string[] input);
        public ImageMemeCommands(ILogger<HomieBot> logger, IConfiguration configuration,IImageStore imageStore, IImageProcessor imageProcessor, Random random)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.imageStore = imageStore;
            this.imageProcessor = imageProcessor;
            this.random = random;
            this.templates = configuration.GetSection("MemeTemplates").Get<IEnumerable<MemeTemplate>>();
            this.collections = configuration.GetSection("ImageCollections").Get<IEnumerable<ImageCollection>>();   
        }

    public async Task ProcessReaction(DiscordClient sender, MessageReactionAddEventArgs messageReaction)
        {
            switch(messageReaction.Emoji.GetDiscordName())
            {
                case ":smoking:":
                    // just fetch the message
                    var message = await messageReaction.Channel.GetMessageAsync(messageReaction.Message.Id);
                    await HomieMessageExtensions.ReactToMessage(message,sender,"never");
                    break;
                default:
                    break;
            }
        }

        public CommandBuilder[] GetDynamicImageCommands()
        {
            var commands = new List<CommandBuilder>();
            foreach(var collection in collections)
            {
                commands.Add(new CommandBuilder()
                    //.WithAlias(gimmick.Command)
                    .WithName(collection.Name)
                    .WithDescription(collection.Description)
                    .WithOverload(
                        new CommandOverloadBuilder(new RunCollection(handleImageCollectionRequest))
                        .WithPriority(0)
                    )
                );
            }
            foreach(var template in templates)
            {
                commands.Add(new CommandBuilder()
                    //.WithAlias(gimmick.Command)
                    .WithName(template.Name)
                    .WithDescription(template.Description)
                    .WithOverload(
                        new CommandOverloadBuilder(new RunMemeTemplate(handleImageMemeRun))
                        .WithPriority(0)
                    )
                );
            }
            return commands.ToArray();
        }

        private async Task<bool> CheckCategory(CommandContext context, string category, bool addable)
        {
            var filtered = addable ? collections.Where(c => c.CanAdd) : collections;
            if(filtered.Any(c=>c.Name.Equals(category,StringComparison.InvariantCultureIgnoreCase))){
                return true;
            }
            else{
                var response = "Couldn't find that image category. Approved image categories for your command are:";
                foreach(var cat in filtered){
                    response += $"\n{cat.Name}";
                }
                _ = await context.RespondAsync(response);
            }
            return false;
        }

        [Command("addimage")]
        [Description("Adds an image to the list for a given command. Valid options are snek and trolly. Should be used like ;;addimage snek <link to image or image attached>")]
        public async Task AddImage(CommandContext context, string category, params string[] args)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            logger.LogInformation("Got an add image command");
            await context.TriggerTypingAsync();
            if(await CheckCategory(context, category, true))
            {
                string imageId;
                string url;
                var coll = collections.FirstOrDefault(c => c.Name.Equals(category,StringComparison.InvariantCultureIgnoreCase));
                // This is valid, lets check if there's an attached image
                if(context.Message.Attachments.Any())
                {
                    var attachment = context.Message.Attachments.FirstOrDefault();
                    imageId = $"{coll.Tag}/{attachment.FileName}";
                    url = attachment.Url;
                }
                else
                {
                    // image wasn't attached, so lets hope its in the args
                    if (args != null && !string.IsNullOrWhiteSpace(args[0]) && args[0].ToLowerInvariant().StartsWith("http"))
                    {
                        System.Uri.TryCreate(args[0], UriKind.Absolute, out Uri attachment);
                        var filename = attachment.AbsolutePath.Split('/').LastOrDefault();
                        imageId = $"{coll.Tag}/{filename}";
                        url = args[0];
                    }
                    else
                    {
                        _ = context.RespondAsync("I couldn't find an image to add, sorry");
                        commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "404")?.Stop();
                        return;
                    }
                }
                var stream = await getStreamFromUrl(url);
                var result = await imageStore.StoreImageAsync(imageId,stream);
                if(result){
                    _ = await context.RespondAsync($"Added image to the {coll.Name} collection");
                    commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
                }else{
                    _ = await context.RespondAsync($"Image didn't add successfully");
                    commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "imageaddfail")?.Stop();
                }
            }
        }
        [Command("never")]
        public async Task GenerateNeverImage(CommandContext context, params string[] args)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            var sourceUrl = await getAttachedImageUrl(context.Message, args);
            if(string.IsNullOrWhiteSpace(sourceUrl)){
                commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "404")?.Stop();
                await context.RespondAsync("Couldn't get an image to wander into.");
            }
            var overlay = await GetOverlaidNeverImage(await getStreamFromUrl(sourceUrl));
            await context.RespondAsync(
                bld => bld.WithContent(
                    "Never shoulda smoked that shit homie, now look where I am"
                    ).AddFile(
                    "nevershoulda.png",
                    new MemoryStream(overlay)
                )
            );
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
        }

        [Command("count")]
        [Description("Counts how many images are in an image tag")]
        public async Task CountImages(CommandContext context, string category)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            if(! await CheckCategory(context, category, false)){
                commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "invalidcategory")?.Stop();
                return;
            }
            ImageCollection collection = collections.Where(c=>c.Name.Equals(category,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            await context.RespondAsync($"There are {await imageStore.GetTaggedImageCountAsync(collection.Tag)} images in the {category} collection");
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
        }
        
        [Command("list")]
        [Description("Lists (as a file attachement) all the images in an image tag")]
        public async Task ListImages(CommandContext context, string category)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            if(! await CheckCategory(context, category, false)){
                commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "invalidcategory")?.Stop();
                return;
            }
            ImageCollection collection = collections.Where(c=>c.Name.Equals(category,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            var fileName = Path.GetTempFileName();
            await File.WriteAllLinesAsync(fileName, imageStore.GetTaggedImageIds(collection.Tag), System.Text.Encoding.UTF8);
            var respFile = File.OpenRead(fileName);
            await context.RespondAsync(
                bld => bld.WithContent(
                    $"Here's everything in the {category} collection"
                    ).AddFile($"{category}.list.txt", respFile)
            );
            respFile.Close();
            await respFile.DisposeAsync();
            File.Delete(fileName);
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
        }
        
        [Command("collect")]
        [Description("Collects alll of the images in an image tag into a zip file")]
        public async Task CollectImages(CommandContext context, string category)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            await context.TriggerTypingAsync();
            if(! await CheckCategory(context, category, false)){
                return;
            }
            ImageCollection collection = collections.Where(c=>c.Name.Equals(category,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            using var memStream = new MemoryStream();
            using var zipFile = new ZipArchive(memStream, ZipArchiveMode.Create, true);
            await foreach(var image in imageStore.GetTaggedImagesAsync(collection.Tag)){
                var entry = zipFile.CreateEntry(image.ImageIdentifier, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(await image.GetBytes());
                entryStream.Close();
            }
            zipFile.Dispose();
            memStream.Position = 0;
            await context.RespondAsync(bld => bld.WithContent($"Here's everything in the {category} collection").AddFile($"{category}.zip", memStream, true));
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
        }

        private async Task<byte[]> GetOverlaidNeverImage(Stream sourceImage)
        {
            var neverImage = await imageStore.GetImageAsync("baseimages/never.png");
            using var ms = new MemoryStream(await neverImage.GetBytes());
            return await imageProcessor.OverlayImage(sourceImage, ms);
        }

        private async Task<string> getAttachedImageUrl(DiscordMessage message, params string[] args)
        {
            string url = string.Empty;
            if(message.Attachments.Any())
                {
                    var attachment = message.Attachments.FirstOrDefault();
                    url = attachment.Url;
                }
            else
            {
                // image wasn't attached, so lets hope its in the args
                if (args != null && args.Length >0 && !string.IsNullOrWhiteSpace(args[0]) && args[0].ToLowerInvariant().StartsWith("http"))
                {
                    if(!checkURLIsImage(args[0])){
                        return url;
                    }
                    System.Uri.TryCreate(args[0], UriKind.Absolute, out Uri attachment);
                    url = args[0];
                }
                else
                {
                    // We didn't have args, so lets check the message body just in case
                    string fullmessage = message.Content;
                    if(!string.IsNullOrEmpty(fullmessage) && fullmessage.ToLowerInvariant().Contains("http"))
                    {
                        // I did not know that null was a "any whitespace" split, how cool is that?
                        string relevant = (fullmessage[fullmessage.ToLowerInvariant().IndexOf("http")..]).Split(null)[0];
                        url = checkURLIsImage(relevant) ? relevant : string.Empty;
                    }
                }
            }
            return url;
        }

        private bool checkURLIsImage(string url) => knownImageEndings.Any( e => url.EndsWith(e));

        private async Task handleImageCollectionRequest(CommandContext context, params string[] input)
        {
            using var commandRun = TelemetryHelpers.StartActivity(context.Command.Name);
            ImageCollection collection = collections.Where(c=>c.Name.Equals(context.Command.Name,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            logger.LogInformation("Got a request for an image collection item: {collectionName}",collection.Name);
            await context.TriggerTypingAsync();
            if(input.Length > 0){
                var imageTag = string.Join(' ', input);
                try
                {
                    var selectimage = await imageStore.GetImageAsync(imageTag);
                    if(selectimage != null)
                    {
                        await sendImage(selectimage, context, collection);
                        return;
                    }
                }
                catch(Exception e)
                {
                    commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, e.Message)?.Stop();
                    await context.RespondAsync($"Sorry homie, couldn't find an image by id: {imageTag}, have this one instead");
                }
            }
            var image = await imageStore.GetRandomTaggedImageAsync(collection.Tag);
            await sendImage(image, context, collection);
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
        }

        private async Task sendImage(IImage image, CommandContext context, ImageCollection collection)
        {
            using var stream = new MemoryStream(await image.GetBytes());
            _ = await context.Message.RespondAsync(bld => {
                bld.WithContent(image.ImageIdentifier).AddFile(image.ImageIdentifier, stream);
            });
            if(!string.IsNullOrWhiteSpace(collection.PostText))
            {
                _ = await context.Channel.SendMessageAsync(collection.PostText);
            }
        }

        private async Task handleImageMemeRun(CommandContext ctx, params string[] args)
        {
            using var commandRun = TelemetryHelpers.StartActivity(ctx.Command.Name);
            MemeTemplate memeTemplate = templates.Where(t=>t.Name.Equals(ctx.Command.Name,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            logger.LogInformation("Got a request for an {templateName} imagememe",memeTemplate.Name);
            await ctx.TriggerTypingAsync();
            var meme = new ImageMeme {
                Template = memeTemplate
            };
            var stream = memeTemplate.SingleReplacement ?  new MemoryStream(
                    await meme.GetImageAsync(imageProcessor, string.Join(" ",args))
                ) : new MemoryStream( await meme.GetImageAsync(imageProcessor, args)
                );

            await ctx.Message.RespondAsync(bld => {
                bld.AddFile($"{memeTemplate.Name}.jpg", stream);
            });
            commandRun?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok).Stop();
        }

        private async Task<Stream> getStreamFromUrl(string url)
        {
            using var httpClient = new HttpClient();
            using var file = await httpClient.GetStreamAsync(url).ConfigureAwait(false);
            var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream;
        }
    }
}
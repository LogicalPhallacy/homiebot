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

namespace Homiebot.Discord.Commands
{
    class ImageMemeCommands : BaseCommandModule
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        private IEnumerable<MemeTemplate> templates;
        private IEnumerable<ImageCollection> collections;
        private readonly IImageStore imageStore;
        private readonly Random random;
        private readonly IImageProcessor imageProcessor;
        public delegate Task RunMemeTemplate(CommandContext ctx, params string[] input);
        public delegate Task RunCollection(CommandContext ctx);
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

        // [Command("homies")]
        // [Description("Image version of the homies meme")]
        // public async Task HomiesMeme(CommandContext ctx, params string[] args) 
        // {
        //     logger.LogInformation("Got a request for a homies imagememe");
        //     await ctx.TriggerTypingAsync();
        //     var homiesMeme = new ImageMeme {
        //         Template = templates.Where(t => t.Name == "homies").FirstOrDefault()
        //     };
        //     var stream = new MemoryStream(
        //             await homiesMeme.GetImageAsync(
        //                 imageStore,random,string.Join(" ",args)
        //             )
        //         );
        //     await ctx.Message.RespondAsync(bld => {
        //         bld.WithFile("homies.jpg", stream);
        //     });
        //     //await ctx.RespondWithFileAsync("homies.jpg", new MemoryStream(await homiesMeme.GetImageAsync(imageStore, random, string.Join(" ",args))));
        // }
        // [Command("doof")]
        // [Description("behold the image-meme-inator")]
        // public async Task DoofMeme(CommandContext ctx, params string[] args) 
        // {
        //     logger.LogInformation("Got a request for a inator imagememe");
        //     await ctx.TriggerTypingAsync();
        //     var doofMeme = new ImageMeme {
        //         Template = templates.Where(t => t.Name == "doof").FirstOrDefault()
        //     };
        //     var stream = new MemoryStream(
        //             await doofMeme.GetImageAsync(
        //                 imageStore,random,string.Join(" ",args)
        //             )
        //         );
        //     await ctx.Message.RespondAsync(bld => {
        //         bld.WithFile("doof.jpg", stream);
        //     });
        //     //await ctx.RespondWithFileAsync("homies.jpg", new MemoryStream(await homiesMeme.GetImageAsync(imageStore, random, string.Join(" ",args))));
        // } 
        // [Command("spongebob")]
        // [Description("Have spongebob mock something")]
        // public async Task Spongebob(CommandContext ctx, params string[] args) 
        // {
        //     logger.LogInformation("Got a request for a spongebob imagememe");
        //     await ctx.TriggerTypingAsync();
        //     var bobMeme = new ImageMeme {
        //         Template = templates.Where(t => t.Name == "spongebob").FirstOrDefault()
        //     };
        //     using var stream = new MemoryStream(
        //             await bobMeme.GetImageAsync(
        //                 imageStore,random,string.Join(" ",args)
        //             ));
        //     await ctx.Message.RespondAsync( bld => {
        //         bld.WithFile("mocking.jpg", 
        //         stream
        //         );
        //     });
        //     //await ctx.RespondWithFileAsync("mocking.jpg", new MemoryStream(await bobMeme.GetImageAsync(imageStore, random, string.Join(" ",args))));
        // }
        // [Command("trolly")]
        // [Description("Gets you a trolly problem")]
        // public async Task TrollyProblem(CommandContext context)
        // {
        //     logger.LogInformation("Got a request for a trolly");
        //     await context.TriggerTypingAsync();
        //     var image = await imageStore.GetRandomTaggedImageAsync("trolly");
        //     using var stream = new MemoryStream(await image.GetBytes());
        //     await context.Message.RespondAsync(bld => {
        //         bld.WithFile(image.ImageIdentifier, stream);
        //     });
        //     await context.RespondAsync("Ding Ding");
        // }
        // [Command("snek")]
        // [Description("Don't tread on me!")]
        // public async Task Snek(CommandContext context)
        // {
        //     logger.LogInformation("Got a request for a snek flag");
        //     await context.TriggerTypingAsync();
        //     var image = await imageStore.GetRandomTaggedImageAsync("snekflags");
        //     using var stream = new MemoryStream(await image.GetBytes());
        //     await context.Message.RespondAsync(bld => {
        //         bld.WithFile(image.ImageIdentifier, stream);
        //     });
        //     //await context.RespondWithFileAsync(image.ImageIdentifier, new MemoryStream(await image.GetBytes()));
        // }

        [Command("addimage")]
        [Description("Adds an image to the list for a given command. Valid options are snek and trolly. Should be used like ;;addimage snek <link to image or image attached>")]
        public async Task AddImage(CommandContext context, string category, params string[] args)
        {
            logger.LogInformation("Got an add image command");
            await context.TriggerTypingAsync();
            var canAdd = collections.Where(c => c.CanAdd);
            var requested = canAdd.Where(c=>c.Name.Equals(category,StringComparison.InvariantCultureIgnoreCase));
            if(requested.Any())
            {
                string imageId;
                string url;
                var coll = requested.FirstOrDefault();
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
                        return;
                    }
                }
                var stream = await getStreamFromUrl(url);
                var result = await imageStore.StoreImageAsync(imageId,stream);
                if(result){
                    _ = await context.RespondAsync($"Added image to the {coll.Name} collection");
                }else{
                    _ = await context.RespondAsync($"Image didn't add successfully");
                }
            }
            else
            {
                var response = "Couldn't find that command on the approved to add list. Approved Image Categories Are:";
                foreach(var cat in canAdd){
                    response += $"\n{cat.Name}";
                }
                _ = context.RespondAsync(response);
            }
        }
        private async Task handleImageCollectionRequest(CommandContext context)
        {
            ImageCollection collection = collections.Where(c=>c.Name.Equals(context.Command.Name,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            logger.LogInformation("Got a request for an image collection item: {collectionName}",collection.Name);
            await context.TriggerTypingAsync();
            var image = await imageStore.GetRandomTaggedImageAsync(collection.Tag);
            using var stream = new MemoryStream(await image.GetBytes());
            _ = await context.Message.RespondAsync(bld => {
                bld.WithFile(image.ImageIdentifier, stream);
            });
            if(!string.IsNullOrWhiteSpace(collection.PostText))
            {
                _ = await context.Channel.SendMessageAsync(collection.PostText);
            }
        }

        private async Task handleImageMemeRun(CommandContext ctx, params string[] args)
        {
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
                bld.WithFile($"{memeTemplate.Name}.jpg", stream);
            });
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
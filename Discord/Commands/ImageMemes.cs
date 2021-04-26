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
using Homiebot.Models;
using Homiebot.Images;

namespace Homiebot.Discord.Commands
{
    class ImageMemeCommands : BaseCommandModule
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        private IEnumerable<MemeTemplate> templates;
        private IImageStore imageStore;
        private Random random;
        public ImageMemeCommands(ILogger<HomieBot> logger, IConfiguration configuration,IImageStore imageStore, Random random)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.imageStore = imageStore;
            this.random = random;
            this.templates = configuration.GetSection("MemeTemplates").Get<IEnumerable<MemeTemplate>>();
        }
        [Command("homies")]
        [Description("Image version of the homies meme")]
        public async Task HomiesMeme(CommandContext ctx, params string[] args) 
        {
            logger.LogInformation("Got a request for a homies imagememe");
            await ctx.TriggerTypingAsync();
            var homiesMeme = new ImageMeme {
                Template = templates.Where(t => t.Name == "homies").FirstOrDefault()
            };
            var stream = new MemoryStream(
                    await homiesMeme.GetImageAsync(
                        imageStore,random,string.Join(" ",args)
                    )
                );
            await ctx.Message.RespondAsync(bld => {
                bld.WithFile("homies.jpg", stream);
            });
            //await ctx.RespondWithFileAsync("homies.jpg", new MemoryStream(await homiesMeme.GetImageAsync(imageStore, random, string.Join(" ",args))));
        }

        [Command("spongebob")]
        [Description("Have spongebob mock something")]
        public async Task Spongebob(CommandContext ctx, params string[] args) 
        {
            logger.LogInformation("Got a request for a spongebob imagememe");
            await ctx.TriggerTypingAsync();
            var bobMeme = new ImageMeme {
                Template = templates.Where(t => t.Name == "spongebob").FirstOrDefault()
            };
            var stream = new MemoryStream(
                    await bobMeme.GetImageAsync(
                        imageStore,random,string.Join(" ",args)
                    ));
            await ctx.Message.RespondAsync( bld => {
                bld.WithFile("mocking.jpg", 
                stream
                );
            });
            //await ctx.RespondWithFileAsync("mocking.jpg", new MemoryStream(await bobMeme.GetImageAsync(imageStore, random, string.Join(" ",args))));
        }
        /*
        [Command("trolly")]
        [Description("Gets you a trolly problem")]
        public async Task TrollyProblem(CommandContext context)
        {
            logger.LogInformation("Got a request for a trolly");
            await context.TriggerTypingAsync();
            var image = await imageStore.GetRandomTaggedImageAsync("trolly");
            await context.RespondWithFileAsync(image.ImageIdentifier, new MemoryStream(await image.GetBytes()));
            await context.RespondAsync("Ding Ding");
        }
        */
        [Command("snek")]
        [Description("Don't tread on me!")]
        public async Task Snek(CommandContext context)
        {
            logger.LogInformation("Got a request for a snek flag");
            await context.TriggerTypingAsync();
            var image = await imageStore.GetRandomTaggedImageAsync("snekflags");
            var stream = new MemoryStream(await image.GetBytes());
            await context.Message.RespondAsync(bld => {
                bld.WithFile(image.ImageIdentifier, stream);
            });
            //await context.RespondWithFileAsync(image.ImageIdentifier, new MemoryStream(await image.GetBytes()));
        }
    }
}
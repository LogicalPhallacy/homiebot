using System.Threading.Tasks;
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
using homiebot.images;

namespace homiebot 
{
    class ImageMemeCommands : BaseCommandModule
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        private ImageMeme homiesMeme;
        private IImageStore imageStore;

        public ImageMemeCommands(ILogger<HomieBot> logger, IConfiguration configuration,IImageStore imageStore)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.imageStore = imageStore;
            homiesMeme = new ImageMeme 
            {
                Template = new MemeTemplate
                {
                    ImageBase = new LocallyStoredFile()
                    {
                        Identifier = "images/homies.jpg",
                        Name = "homies"
                    },
                    memeText = new MemeText[] {
                        new MemeText
                        {
                            TemplateText = "Fuck @REPLACEMENT1@!",
                            Height = 75,
                            Width = 620,
                            XStartPosition = 20,
                            YStartPosition = 20
                        },
                        new MemeText
                        {
                            TemplateText = "All my homies hate @REPLACEMENT1@.",
                            Height = 100,
                            Width = 700,
                            XStartPosition = 20,
                            YStartPosition = 550
                        },
                    }
                }
            };
        }
        [Command("homies")]
        [Description("Image version of the homies meme")]
        public async Task HomiesMeme(CommandContext ctx, params string[] args) 
        {
            logger.LogInformation("Got a request for a homies imagememe");
            await ctx.TriggerTypingAsync();
            await ctx.RespondWithFileAsync("homies.jpg", new MemoryStream(await homiesMeme.GetImageAsync(string.Join(" ",args))));
        }
        
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

        [Command("snek")]
        [Description("Don't tread on me!")]
        public async Task Snek(CommandContext context)
        {
            logger.LogInformation("Got a request for a snek flag");
            await context.TriggerTypingAsync();
            var image = await imageStore.GetRandomTaggedImageAsync("snekflags");
            await context.RespondWithFileAsync(image.ImageIdentifier, new MemoryStream(await image.GetBytes()));
        }
    }
}
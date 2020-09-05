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

namespace homiebot 
{
    class ImageMemeCommands : BaseCommandModule
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        private ImageMeme homiesMeme;

        public ImageMemeCommands(ILogger<HomieBot> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
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
        public async Task HomiesMeme(CommandContext ctx, params string[] args) 
        {
            logger.LogInformation("Got a request for a homies imagememe");
            await ctx.TriggerTypingAsync();
            await ctx.RespondWithFileAsync("homies.jpg", new MemoryStream(await homiesMeme.GetImageAsync(args)));
        }
    }
}
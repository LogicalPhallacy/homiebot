using System;
using System.IO;
using System.Threading.Tasks;
using Homiebot.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;


namespace Homiebot.Images
{
    public class ImageSharpImageProcessor : IImageProcessor
    {
        private readonly IImageStore imageStore;
        private readonly Random random;
        public ImageSharpImageProcessor(IImageStore imageStore, Random random)
        {
            this.imageStore = imageStore;
            this.random = random;
        }

        private async Task<Font> getFont(MemeText m, string text)
        {
            var fam = SystemFonts.Find("Impact");
            var basefont = new Font(fam,12, FontStyle.Bold);
            var imagebox = await Task.Run(
                () => TextMeasurer.Measure(text, new RendererOptions(basefont))
                );
            var yscalefactor = m.Height / imagebox.Height;
            var xscalefactor = m.Width / imagebox.Width;
            return new Font(basefont, 12*(xscalefactor < yscalefactor ? xscalefactor : yscalefactor));
        }

        private Size GetSize(int overlayWidth, int overlayHeight, int sourceWidth, int sourceHeight)
        {
            float yscalefactor = (float)sourceHeight/(float)overlayHeight;
            float xscalefactor = (float)sourceWidth/(float)overlayWidth;
            float resizepercent = xscalefactor < yscalefactor ? xscalefactor/2 : yscalefactor/2;
            int width = ((int)Math.Round(overlayWidth*resizepercent,0));
            int height = ((int)Math.Round(overlayHeight*resizepercent,0));
            return new Size(width, height);
        }

        private PointF findCenter(MemeText m)
        {
            return new PointF(
                (m.Width/2)+m.XStartPosition,
                (m.Height/2)+m.YStartPosition
            );
        }

        private Point findXCenter(int sourceWidth, int overlayWidth, int sourceHeight)
        {
            int xpos = (sourceWidth-overlayWidth)/2;
            return new Point(xpos,sourceHeight);
        }
        private Point findXOffCenter(int sourceWidth, int overlayWidth, int sourceHeight)
        {
            int xpos = (sourceWidth-overlayWidth)/2;
            return new Point(xpos-((xpos*2)/3),sourceHeight);
        }

        public async Task<byte[]> ProcessImage(ImageMeme meme, params string[] replacements)
        {
            var imagebytes = await(await imageStore.GetImageAsync(meme.Template.ImageBaseIdentifier)).GetBytes();
            using var image = Image.Load(imagebytes);
            foreach(var text in meme.Template.memeText)
            {
                // The options are optional
                
                TextOptions options = new TextOptions()
                {
                    ApplyKerning = true,
                    TabWidth = 8, // a tab renders as 8 spaces wide
                    //WrapTextWidth = 100, // greater than zero so we will word wrap at 100 pixels wide
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                IBrush brush = Brushes.Solid(Color.Parse(text.FillColor));
                IPen pen = Pens.Solid(Color.Parse(text.OutlineColor),2);
                //string text = "sample text";
                string words = text.GetMemeText(random, replacements);
                // draws a star with Horizontal red and blue hatching with a dash dot pattern outline.
                Font f = await getFont(text, words);
                await Task.Run(
                    () => image.Mutate( 
                        x=> x.DrawText(
                            new DrawingOptions(){
                                TextOptions = options
                            }, 
                            words,
                            f, 
                            brush, 
                            pen, findCenter(text)
                        )
                    )
                );
            }
            using (var ms = new MemoryStream())
            {
                image.Save(ms,new PngEncoder());
                return ms.ToArray();
            }
        }

        public async Task<byte[]> OverlayImage(Stream baseImage, Stream overlayImage)
        {
            baseImage.Position = 0;
            overlayImage.Position = 0;
            using var image = Image.Load(baseImage);
            using var overlay = Image.Load(overlayImage);
            // resize the overlay image
            await Task.Run(() =>overlay.Mutate(
                i => i.Resize(
                    GetSize(overlay.Width, overlay.Height, image.Width, image.Height)
                )
            ));
            // overlay the resized image
            await Task.Run( () => 
                image.Mutate(
                i => i.DrawImage(
                    overlay,
                    findXOffCenter(image.Width, overlay.Width, image.Height-overlay.Height),
                    1f
                )
                ));
            using (var ms = new MemoryStream())
            {
                image.Save(ms,new PngEncoder());
                return ms.ToArray();
            }
        }
    }

}
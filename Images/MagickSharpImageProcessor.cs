using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Homiebot.Models;
using Homiebot.Web;
using ImageMagick;
using ImageMagick.Factories;

namespace Homiebot.Images
{
    public class MagickSharpImageProcessor : IImageProcessor
    {
        private readonly IImageStore imageStore;
        private readonly Random random;

        public MagickSharpImageProcessor(IImageStore imageStore, Random random)
        {
            this.imageStore = imageStore;
            this.random = random;
        }
        public MagickReadSettings Composition(MemeText m)
        {
            return new MagickReadSettings(){
                Font = "Impact",
                TextGravity = Gravity.Center,
                BackgroundColor = MagickColors.Transparent,
                StrokeColor = new MagickColor(m.OutlineColor),
                FillColor = new MagickColor(m.FillColor),
                Height = (uint)m.Height,
                Width = (uint)m.Width
            };
        }

        public async Task<byte[]> OverlayImage(Stream baseImage, Stream overlayImage)
        {
            using var overlayActivity = TelemetryHelpers.StartActivity("GenerateImageOverlay", System.Diagnostics.ActivityKind.Internal);
            var factory = new MagickImageFactory();
            baseImage.Position = 0;
            overlayImage.Position = 0;
            using var image = factory.Create(baseImage);
            using var overlay = factory.Create(overlayImage);
            var scale = findScalePercentage(overlay.Width, overlay.Height, image.Width, image.Height);
            using (var scalingImage = TelemetryHelpers.StartActivity("ScaleImage", System.Diagnostics.ActivityKind.Internal)){
                await Task.Run( () => overlay.Scale(scale));
                scalingImage?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
            }
            
            using (var compositing = TelemetryHelpers.StartActivity("CompositeOverlay", System.Diagnostics.ActivityKind.Internal)){
                await Task.Run( () => image.Composite(overlay,
                findXOffCenter(overlay.Width, image.Width),
                findYBottom(overlay.Height, image.Height),
                CompositeOperator.Over));
                compositing?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
            }
            
            var ret = await Task.Run<byte[]>(()=>{return image.ToByteArray();});
            overlayActivity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok)?.Stop();
            return ret;
        }

        private Percentage findScalePercentage(int overlayWidth, int overlayHeight, int sourceWidth, int sourceHeight)
        {
            double yscalefactor = (double)sourceHeight / (double)overlayHeight;
            double xscalefactor =  (double)sourceWidth / (double)overlayWidth;
            return new Percentage(xscalefactor < yscalefactor ? xscalefactor * 50 : yscalefactor * 50);
        }
        private Percentage findScalePercentage(uint overlayWidth, uint overlayHeight, uint sourceWidth, uint sourceHeight)
        {
            double yscalefactor = (double)sourceHeight / (double)overlayHeight;
            double xscalefactor =  (double)sourceWidth / (double)overlayWidth;
            return new Percentage(xscalefactor < yscalefactor ? xscalefactor * 50 : yscalefactor * 50);
        }

        private int findXCenter(int overlayWidth, int sourceWidth)
        {
            return (sourceWidth-overlayWidth)/2;
        }
        private int findXOffCenter(int overlayWidth, int sourceWidth)
        {
            int xpos = ((sourceWidth-overlayWidth)/2);
            return (xpos - ((xpos*2)/3));
        }
        private int findYBottom(int overlayHeight, int sourceHeight) => sourceHeight-overlayHeight;
        private uint findXCenter(uint overlayWidth, uint sourceWidth)
        {
            return (sourceWidth-overlayWidth)/2;
        }
        private int findXOffCenter(uint overlayWidth, uint sourceWidth)
        {
            uint xpos = ((sourceWidth-overlayWidth)/2);
            return (int)(xpos - ((xpos*2)/3));
        }
        private int findYBottom(uint overlayHeight, uint sourceHeight) => (int)(sourceHeight-overlayHeight);

        public async Task<byte[]> ProcessImage(ImageMeme meme, params string[] replacements)
        {
            var factory = new MagickImageFactory();
            using (var image = factory.Create(await(await imageStore.GetImageAsync(meme.Template.ImageBaseIdentifier)).GetBytes()))
            {
                List<MagickImage> MemeTexts = new List<MagickImage>(); 
                foreach(var m in meme.Template.memeText)
                {
                    MagickImage mti;
                    
                    try
                    {
                        mti = new MagickImage($"caption:{m.GetMemeText(random, replacements)}", Composition(m));
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                    
                    MemeTexts.Add(mti);
                    // well there's your problem
                    // image.Composite(mti,m.XStartPosition,m.YStartPosition,CompositeOperator.Over);
                    await Task.Run( () => {
                        image.Composite(mti,m.XStartPosition,m.YStartPosition,CompositeOperator.Over);
                    });
                    //

                }
                var bytes = await Task.Run<byte[]>(()=>{return image.ToByteArray();});
                // dispose our dynamic images
                //image.Write(writeStream);
                foreach(var m in MemeTexts)
                {
                    m.Dispose();
                }
                return bytes;
            }
        }
    }
}
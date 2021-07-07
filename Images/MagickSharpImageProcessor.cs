using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Homiebot.Models;
using ImageMagick;

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
                Height = m.Height,
                Width = m.Width
            };
        }

        public async Task<byte[]> OverlayImage(Stream baseImage, Stream overlayImage)
        {
            var factory = new MagickImageFactory();
            using var image = factory.Create(baseImage);
            using var overlay = factory.Create(overlayImage);
            await Task.Run( () => overlay.Scale(findScalePercentage(overlay.Width, overlay.Height, image.Width, image.Height)));
            await Task.Run( () => image.Composite(overlay,findXCenter(overlay.Width, image.Width), 0, CompositeOperator.Over));
            return await Task.Run<byte[]>(()=>{return image.ToByteArray();});
        }

        private Percentage findScalePercentage(int scaleWidth, int scaleHeight, int sourceWidth, int sourceHeight)
        {
            var yscalefactor = scaleHeight / sourceHeight;
            var xscalefactor = scaleWidth / sourceWidth;
            return new Percentage(xscalefactor < yscalefactor ? xscalefactor : yscalefactor);
        }

        private int findXCenter(int overlayWidth, int sourceWidth)
        {
            return (sourceWidth-overlayWidth)/2;
        }

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
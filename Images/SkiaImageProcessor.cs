using System;
using System.IO;
using System.Threading.Tasks;
using Homiebot.Models;

namespace Homiebot.Images
{
    public class SkiaImageProcessor : IImageProcessor
    {
        private readonly IImageStore imageStore;
        private readonly Random random;
        public SkiaImageProcessor(IImageStore imageStore, Random random)
        {
            this.imageStore = imageStore;
            this.random = random;
        }

        public Task<byte[]> OverlayImage(Stream baseImage, Stream overlayImage)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ProcessImage(ImageMeme meme, params string[] replacements)
        {
            throw new System.NotImplementedException();
        }
    }

}
using System.IO;
using System.Threading.Tasks;
using Homiebot.Models;

namespace Homiebot.Images
{
    public interface IImageProcessor
    {
        Task<byte[]> ProcessImage(ImageMeme meme, params string[] replacements);
        Task<byte[]> OverlayImage(Stream baseImage, Stream overlayImage);
    }
}
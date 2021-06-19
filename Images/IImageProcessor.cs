using System.Threading.Tasks;
using Homiebot.Models;

namespace Homiebot.Images
{
    public interface IImageProcessor
    {
        public Task<byte[]> ProcessImage(ImageMeme meme, params string[] replacements);
    }
}
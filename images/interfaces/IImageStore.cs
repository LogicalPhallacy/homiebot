using System;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace homiebot.images 
{
    public interface IImageStore 
    {
        IAsyncEnumerable<IImage> GetTaggedImagesAsync(string tag);
        Task<IImage> GetRandomTaggedImageAsync(string tag);
        Task<IImage> GetImageAsync(string ImageId);
    }
}
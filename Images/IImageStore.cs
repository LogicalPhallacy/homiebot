using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace Homiebot.Images 
{
    public interface IImageStore 
    {
        IAsyncEnumerable<IImage> GetTaggedImagesAsync(string tag);
        Task<int> GetTaggedImageCountAsync(string tag);
        IEnumerable<string> GetTaggedImageIds(string tag);
        Task<IImage> GetRandomTaggedImageAsync(string tag);
        Task<IImage> GetImageAsync(string ImageId);
        Task<bool> StoreImageAsync(string ImageId, Stream file);
    }
}
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Homiebot.Models;
using Homiebot;

namespace Homiebot.Images
{
    public class LocalImageStore : IImageStore
    {
        private readonly ILogger logger;
        private readonly Random random;
        private readonly IConfiguration configuration;
        private string basePath;
        private Dictionary<string,HashSet<string>> usedByTag;
        public LocalImageStore(ILogger<HomieBot> logger, IConfiguration configuration, Random random)
        {
            var conf = configuration.GetSection("LocalImageStoreConfig").Get<LocalImageStoreConfig>();
            basePath = conf.ImageStorePath;
            this.logger = logger;
            this.random = random;
            this.configuration = configuration;
            usedByTag = new();
        }
        public Task<IImage> GetImageAsync(string ImageId)
        {
            if(File.Exists(Path.Join(basePath,ImageId)))
            {
                return Task.FromResult<IImage>(new LocalImage(basePath,ImageId));
            }
            return null;
        }

        public async Task<Stream> GetListFileForTagAsync(string tag)
        {
            if(Directory.Exists(Path.Join(basePath,tag)))
            {
                var list = GetDirectoryImages(tag);
                var tempfile = Path.GetTempFileName();
                await File.WriteAllLinesAsync(tempfile, list, System.Text.Encoding.UTF8);
                return File.OpenRead(tempfile);
            }
            else
                return Stream.Null;
        }

        public Task<IImage> GetRandomTaggedImageAsync(string tag)
        {
            HashSet<string> used;
            if(!usedByTag.ContainsKey(tag)){
                used = new();
                usedByTag.Add(tag, used);
            }
            used = usedByTag[tag];
            if(Directory.Exists(Path.Join(basePath,tag)))
            {
                var item = GetDirectoryImages(tag).GetRandomUnused(random,ref used);
                used.Add(item.Replace(Path.GetFullPath(basePath),""));
                usedByTag[tag] = used;
                return Task.FromResult<IImage>(new LocalImage(basePath,item));
            }
            return null;
        }

        public async Task<int> GetTaggedImageCountAsync(string tag)
        {
            if(Directory.Exists(Path.Join(basePath,tag)))
            {
                return GetDirectoryImages(tag).Count();
            }
            return 0;
        }

        public IEnumerable<string> GetTaggedImageIds(string tag)
            => Directory.Exists(Path.Join(basePath,tag)) ?
                GetDirectoryImages(tag) :
                new string[0];

        public async IAsyncEnumerable<IImage> GetTaggedImagesAsync(string tag)
        {
            if(!Directory.Exists(Path.Join(basePath,tag))){
                yield break;
            }
            foreach(var item in GetDirectoryImages(tag))
            {
                yield return new LocalImage(basePath,item);
            }
        }

        public async Task<bool> StoreImageAsync(string ImageId, Stream file)
        {
            using var writeFile = File.OpenWrite(Path.Join(basePath,ImageId));
            // TODO: Handle errors
            await file.CopyToAsync(file);
            return true;
        }

        private IEnumerable<string> GetDirectoryImages(string directory)
        {
            return stripFilePaths(Directory.GetFiles(Path.Join(basePath,directory)));
        }
        private string stripFilePath(string filePath)
        {
            return filePath.Replace(Path.GetFullPath(basePath),"");
        }
        private IEnumerable<string> stripFilePaths(IEnumerable<string> filePaths)
        {
            return filePaths.Select(s => stripFilePath(s));
        }
    }
}
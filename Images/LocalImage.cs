using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Homiebot.Images
{
    public class LocalImage : IImage
    {

        private string basePath;
        private string imagePath;
        public LocalImage(string basePath, string relativeFilePath)
        {
            this.basePath = basePath;
            imagePath = relativeFilePath;
        }
        public string ImageIdentifier { get => imagePath; set => imagePath = value; }

        public IEnumerable<string> ImageTags => throw new NotImplementedException();

        public void addTag(string tag)
        {
            throw new NotImplementedException();
        }

        public async Task<byte[]> GetBytes() =>  await File.ReadAllBytesAsync(Path.Join(basePath,imagePath));
    }
}
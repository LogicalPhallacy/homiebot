using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3.Model;
using System.Threading.Tasks;

namespace Homiebot.Images
{
    public class S3BucketImage : IImage
    {
        private GetObjectResponse objectResponse;
        public string ImageIdentifier { get => objectResponse.Key; set => throw new NotImplementedException(); }

        private List<string> tags;
        public IEnumerable<string> ImageTags => tags;
                
        public S3BucketImage(GetObjectResponse bucketres)
        {
            objectResponse = bucketres;
        }
        public void addTag(string tag)
        {
            if(tags == null)
            {
                tags = new List<string>();
            }
            tags.Add(tag);
        }

        public async Task<byte[]> GetBytes()
        {
            using(var memstream = new MemoryStream())
            {
                await objectResponse.ResponseStream.CopyToAsync(memstream);
                return memstream.ToArray();
            }
        }
    }
}
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
namespace homiebot.images
{
    public class AWSS3BucketImageStore : IImageStore
    {
        private AmazonS3Client client;
        private string bucketName;
        private ILogger logger;
        private Random random;
        private IConfiguration configuration;
        public AWSS3BucketImageStore(ILogger<HomieBot> logger, IConfiguration configuration, Random random)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.random = random;
            logger.LogInformation("Getting AWS connection");
            var awsconf = configuration.GetSection("AWSConfig").Get<AWSConfig>();
            //var options = new CredentialProfileOptions
            //{
            //    AccessKey = awsconf.AccessKey,
            //    SecretKey = awsconf.SecretKey
            //};
            //var profile = new Amazon.Runtime.CredentialManagement.CredentialProfile("basic_profile", options);
            //profile.Region = Amazon.RegionEndpoint.USEast2;
            Environment.SetEnvironmentVariable("AWS_REGION",awsconf.Region);
            //var netSDKFile = new NetSDKCredentialsFile();
            //netSDKFile.RegisterProfile(profile);
            client = new AmazonS3Client(awsconf.AccessKey,awsconf.SecretKey);
            bucketName = awsconf.BucketName;
        }
        public async Task<IImage> GetImageAsync(string ImageId)
        {
            var awsobj = await client.GetObjectAsync(bucketName,ImageId);
            if(awsobj.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return new S3BucketImage(awsobj);
            }
            return null;
        }

        public async Task<IImage> GetRandomTaggedImageAsync(string tag)
        {
            ListObjectsV2Request request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = $"{tag}/"
            };
            var things = await client.ListObjectsV2Async(request);
            if(things.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                var chosen = things.S3Objects.ElementAt(random.Next(0,things.KeyCount));
                return await GetImageAsync(chosen.Key);
            }
            return null;
        }

        public async IAsyncEnumerable<IImage> GetTaggedImagesAsync(string tag)
        {
            ListObjectsV2Request request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = $"{tag}/"
            };
            var things = await client.ListObjectsV2Async(request);
            if(things.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                foreach(var s3o in things.S3Objects)
                {
                    yield return await GetImageAsync(s3o.Key);
                }
            }
            else
            {
                throw new Exception($"Bad Response from AWS: {things.HttpStatusCode}");
            }
        }
    }
}
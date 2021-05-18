using System;
using System.Collections.Generic;
using Homiebot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using Homiebot.Images;
using System.Threading.Tasks;
using System.IO;

namespace Homiebot.Web.Controllers
{
    public class ImageController : Controller
    {
        private readonly ILogger<GimmickController> _logger;
        private readonly Random random;
        private readonly IConfiguration config;
        private IImageStore imageStore;

        public class ImageResult 
        {
            public string Name {get;set;}
            public string AltText{get;set;}
        }

        public ImageController(ILogger<GimmickController> logger, Random random, IConfiguration config, IImageStore imageStore)
        {
            _logger = logger;
            this.random = random;
            this.config = config;
            this.imageStore = imageStore;
        }
        [Route("/image/{name}")]
        public async Task<IActionResult> ImageView(string name)
        {
            IImage image = name switch
            {
                "snek"=> await imageStore.GetRandomTaggedImageAsync("snekflags"),
                "trolly"=> await imageStore.GetRandomTaggedImageAsync("trolly"),
                _ => await imageStore.GetRandomTaggedImageAsync("snekflags"),
            };
            return View(new ImageResult{
                Name = image.ImageIdentifier,
                AltText = name
            });
        }
        [Route("/image/imagefile")]
        public async Task<FileResult> ImageFile(string name)
        {
            var image = await imageStore.GetImageAsync(name);
            return File(await image.GetBytes(),"image/jpeg");
        }
    }
}
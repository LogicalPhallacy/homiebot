using System;
using System.Collections.Generic;
using Homiebot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Homiebot.Web.Controllers
{
    public class GimmickController : Controller
    {
        private readonly ILogger<GimmickController> _logger;
        private readonly Random random;
        private readonly IConfiguration config;
        private IEnumerable<Gimmick> Gimmicks;

        public GimmickController(ILogger<GimmickController> logger, Random random, IConfiguration config)
        {
            _logger = logger;
            this.random = random;
            this.config = config;
            Gimmicks = config.GetSection("Gimmicks").Get<IEnumerable<Gimmick>>();
            foreach(var g in Gimmicks)
            {
                g.Inject(random,logger,null);
            }
        }
        [Route("/gimmick/{name}")]
        public IActionResult Gimmick(string name, string? args)
        {
            if(string.IsNullOrWhiteSpace(name) || ! Gimmicks.Select(g=>g.Command).Contains(name))
            {
                return new BadRequestResult();
            }
            var gimmick = Gimmicks.Where(g=>g.Command == name).FirstOrDefault();
            string message;
            if(!string.IsNullOrWhiteSpace(args)){
                message = gimmick.Replace(args.Split(' '));
            }else{
                message = gimmick.Replace();
            }
            
            GimmickRun gr;
            gr.Name = name;
            gr.Message = message;
            return View(gr);
        }
    }
}
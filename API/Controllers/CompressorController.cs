using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompressorController : ControllerBase
    {
        private IWebHostEnvironment Environment;

        public CompressorController(IWebHostEnvironment env)
        {
            Environment = env;
        }

        // GET: api/<CompressorController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<CompressorController>/5
        [Route("/api/compressions")]
        [HttpGet]
        public List<LZW> GetListCompress()
        {
            LZW.LoadHistList(Environment.ContentRootPath);
            return Storage.Instance.HistoryList;
        }

        // POST api/<CompressorController>
        [Route("/api/compress/{name}")]
        [HttpPost]
        public void PostCompress([FromBody] string value)
        {
        }

        // POST api/<CompressorController>
        [Route("/api/decompress")]
        [HttpPost]
        public void PostDecompress([FromBody] string value)
        {

        }

    }
}

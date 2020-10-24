using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        public async Task<IActionResult> PostCompressAsync([FromForm] IFormFile file, string name)
        {
            try
            {
                int i = 1;
                var originalname = name;
                if (!Directory.Exists($"{Environment.ContentRootPath}/Uploads/"))
                {
                    Directory.CreateDirectory($"{Environment.ContentRootPath}/Uploads/");
                }
                while (System.IO.File.Exists($"{Environment.ContentRootPath}/Uploads/{name}"))
                {
                    name = originalname + "(" + i.ToString() + ")";
                    i++;
                }
                await Storage.Instance.lzwCompre.CompressFile(Environment.ContentRootPath, file, name);
                var LZWInfo = new LZW();
                LZWInfo.SetAttributes(Environment.ContentRootPath, file.FileName, name);
                Storage.Instance.HistoryList.Add(LZWInfo);

                return PhysicalFile($"{Environment.ContentRootPath}/Compressions/{name}.lzw", MediaTypeNames.Text.Plain, $"{name}.lzw");
            }
            catch 
            {
                return StatusCode(500);
            }


        }

        // POST api/<CompressorController>
        [Route("/api/decompress")]
        [HttpPost]
        public async Task<IActionResult> PostDecompressAsync([FromForm] IFormFile file)
        {
            try
            {
                LZW.LoadHistList(Environment.ContentRootPath);
                var name = "";
                foreach (var item in Storage.Instance.HistoryList)
                {
                    if ($"{item.CompressedName}.lzw" == file.FileName)
                    {
                        name = item.OriginalName;
                    }
                }
                await Storage.Instance.lzwCompre.DecompressFile(Environment.ContentRootPath, file, name);
                return PhysicalFile($"{Environment.ContentRootPath}/Decompressions/{name}", MediaTypeNames.Text.Plain, name);
            }
            catch
            {
                return StatusCode(500);
            }

        }

    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CustomCompressors.Interfaces
{
    interface ICompressor
    {
        Task CompressFile(string path, IFormFile file, string name);
        Task DecompressFile(string path, IFormFile file, string name);
        string CompressText(string text);
        string DecompressText(string text);
    }
}

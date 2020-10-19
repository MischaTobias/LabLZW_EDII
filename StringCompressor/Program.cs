using System;
using System.Collections.Generic;
using CustomCompressors.Compressors;

namespace StringCompressor
{
    class Program
    {
        static void Main(string[] args)
        {           
            try
            {
                LZWCompress lZW = new LZWCompress();
                Console.WriteLine("Escribe el string a comprimir");
                string text = Console.ReadLine();
                Console.WriteLine("Se ha guardado el string con éxito para comprimir");
                string CompressedText = lZW.CompressString(text);
                Console.WriteLine("El resultado de la compresión es el siguiente:");
                Console.WriteLine(CompressedText);
                Console.ReadLine();
            }
            catch
            {
                Console.WriteLine("Inserte un string válido");
                Console.ReadLine();
                Console.Clear();
            }
        }
    }
}

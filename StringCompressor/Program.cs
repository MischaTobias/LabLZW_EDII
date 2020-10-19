using System;
namespace StringCompressor
{
    class Program
    {
        static void Main(string[] args)
        {
            { }
            try
            {
                //Insert:
                Console.WriteLine("Escribe el string a comprimir");
                string text = Console.ReadLine();

                Console.WriteLine("Se ha guardado el string con éxito para comprimir");
                string CompressedText = Huff.CompressText(text);
                Console.WriteLine("El resultado de la compresión es el siguiente:");
                Console.WriteLine(CompressedText);
            }
    }
}

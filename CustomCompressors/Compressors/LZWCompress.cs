using CustomCompressors.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CustomCompressors.Utilities;
using System.Runtime.ConstrainedExecution;
using System.Buffers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using System.Threading;

namespace CustomCompressors.Compressors
{
    public class LZWCompress : ICompressor
    {
        #region Variables
        Dictionary<string, int> LZWTable = new Dictionary<string, int>();
        Dictionary<int, byte> DecompressLZWTable = new Dictionary<int, byte>();
        List<byte> Differentchar = new List<byte>();
        List<byte> Characters = new List<byte>();
        List<int> NumbersToWrite = new List<int>();
        int MaxValueLength = 0;
        int code = 1;
        #endregion

        private void ResetVariables()
        {
            LZWTable.Clear();
            Differentchar.Clear();
            Characters.Clear();
            NumbersToWrite.Clear();
            MaxValueLength = 0;
            code = 1;
        }

        #region Compression
        private void FillDictionary(byte[] Text)
        {
            //First reading through the text
            string chara = string.Empty;
            foreach (var character in Text)
            {
                chara += Convert.ToChar(character);
                if (!LZWTable.ContainsKey(chara))
                {
                    LZWTable.Add(chara, code);
                    code++;
                    Differentchar.Add(character);
                }
                chara = string.Empty;
            }
        }

        private void Compression(byte[] Text)
        { 
            //Segundo recorrido y asignación de valores
            Characters = Text.ToList();
            string Subchain;
            MaxValueLength = 0;
            while (Characters.Count != 0)
            {
                int i = 0;
                Subchain = Characters.ElementAt(i).ToString();
                if(Characters.Count > 1)
                {
                    while (LZWTable.ContainsKey(Subchain))
                    {
                        if (!LZWTable.ContainsKey(Subchain + Characters.ElementAt(i).ToString()))
                        {
                            NumbersToWrite.Add(LZWTable[Subchain]);
                            if (MaxValueLength < LZWTable[Subchain])
                            {
                                MaxValueLength = LZWTable[Subchain];
                            }
                        }
                        i++;
                        Subchain += Characters.ElementAt(i).ToString();
                    }
                    LZWTable.Add(Subchain, code);
                    code++;
                }
                else
                {
                    NumbersToWrite.Add(LZWTable[Subchain]);
                }
                Characters.RemoveAt(0);
            }

            MaxValueLength = Convert.ToString(MaxValueLength, 2).Length;
        }
       
        public string CompressText(string text)
        {
            var buffer = ByteConverter.ConvertToBytes(text);//falta repetir esto varias veces por si es un texto muy grande
            FillDictionary(buffer);
            Compression(buffer);
            string subcode = "";
            List<byte> returningList = new List<byte>
            {
                Convert.ToByte(MaxValueLength),
                Convert.ToByte(Differentchar.Count())
            };
            foreach (var item in Differentchar)
            {
                returningList.Add(item);
            }
            foreach (var number in NumbersToWrite)
            {
                subcode += Convert.ToString(number, 2);
                while (subcode.Length % MaxValueLength != 0)
                {
                    subcode = "0" + subcode;
                }
                if (subcode.Length >= 8)
                {
                    returningList.Add(Convert.ToByte(subcode.Substring(0, 8), 2));
                    subcode = subcode.Remove(0, 8);
                }
            }
            if (subcode.Length != 0)
            {
                returningList.Add(Convert.ToByte(subcode, 2));
            }
            ResetVariables();
            return ByteConverter.ConvertToString(returningList.ToArray());
        }

        public async Task CompressFile(string path, IFormFile file, string name)
        {
            if (System.IO.File.Exists($"{path}/Uploads/{file.FileName}"))
            {
                System.IO.File.Delete($"{path}/Uploads/{file.FileName}");
            }

            if (System.IO.File.Exists(($"{path}/Compressions/{name}.lzw")))
            {
                System.IO.File.Delete(($"{path}/Compressions/{name}.lzw"));
            }

            using var saver = new FileStream($"{path}/Uploads/{file.FileName}", FileMode.OpenOrCreate);
            await file.CopyToAsync(saver);

            using var reader = new BinaryReader(saver);
            int bufferSize = 2000;
            var buffer = new byte[bufferSize];
            while (saver.Position != saver.Length)
            {
                buffer = reader.ReadBytes(bufferSize);
                FillDictionary(buffer);
            }

            saver.Position = saver.Seek(0, SeekOrigin.Begin);
            while (saver.Position != saver.Length)
            {
                buffer = reader.ReadBytes(bufferSize);
                Compression(buffer);
            }
            reader.Close();
            saver.Close();

            using var fileToWrite = new FileStream($"{path}/Compressions/{name}.lzw", FileMode.OpenOrCreate);
            using var writer = new BinaryWriter(fileToWrite);
            string compressionCode = "";
            writer.Write(Convert.ToByte(MaxValueLength));
            writer.Write(Convert.ToByte(Differentchar.Count()));
            foreach (var item in Differentchar)
            {
                writer.Write(item);
            }
            foreach (var number in NumbersToWrite)
            {
                compressionCode += Convert.ToString(number, 2);
                while (compressionCode.Length != MaxValueLength)
                {
                    compressionCode = "0" + compressionCode;
                }
                if (compressionCode.Length >= 8)
                {
                    writer.Write(Convert.ToByte(compressionCode.Substring(0, 8), 2));
                    compressionCode = compressionCode.Remove(0, 8);
                }
            }
            if (compressionCode.Length != 0)
            {
                writer.Write(Convert.ToByte(compressionCode, 2));
                compressionCode = string.Empty;
            }
            writer.Close();
            fileToWrite.Close();
            ResetVariables();
        }
        #endregion

        #region Decompression

        private byte[] FillDecompressionDictionary(byte[] text)
        {
            for (int i = 0; i < text[1]; i++)
            {
                DecompressLZWTable.Add(code, text[i + 2]);
            }
        }

        private void Decompression(byte[] compressedText)
        {
            
        }

        public string DecompressText(string text)
        {
            var buffer = ByteConverter.ConvertToBytes(text);
            MaxValueLength = buffer[0];
            buffer = FillDecompressionDictionary(buffer);
            ByteConverter.ConvertToString(buffer);
        }

        public async Task DecompressFile(IFormFile file, string name)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

﻿using CustomCompressors.Interfaces;
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
using System.Security.Cryptography;

namespace CustomCompressors.Compressors
{
    public class LZWCompress /*: ICompressor*/
    {
        #region Variables
        Dictionary<string, int> LZWTable = new Dictionary<string, int>();
        Dictionary<int, List<byte>> DecompressLZWTable = new Dictionary<int, List<byte>>();
        List<byte> Differentchar = new List<byte>();
        List<byte> Characters = new List<byte>();
        List<int> NumbersToWrite = new List<int>();
        List<List<byte>> DecompressValues = new List<List<byte>>();
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
                if (!LZWTable.ContainsKey(character.ToString()))
                {
                    LZWTable.Add(character.ToString(), code);
                    code++;
                    Differentchar.Add(character);
                }
                chara = string.Empty;
            }
        }

        //Funciona!
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
            string Ccode = "";
            foreach (var number in NumbersToWrite)
            {
                subcode = Convert.ToString(number, 2);
                while (subcode.Length != MaxValueLength)
                {
                    subcode = "0" + subcode;
                }
                Ccode += subcode;
                if (Ccode.Length >= 8)
                {
                    returningList.Add(Convert.ToByte(Ccode.Substring(0, 8), 2));
                    Ccode = Ccode.Remove(0, 8);
                }
            }
            if (Ccode.Length != 0)
            {
                returningList.Add(Convert.ToByte(Ccode, 2));
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
            string Ccode = "";
            foreach (var number in NumbersToWrite)
            {
                compressionCode = Convert.ToString(number, 2);
                while (compressionCode.Length != MaxValueLength)
                {
                    compressionCode = "0" + compressionCode;
                }
                Ccode += compressionCode;
                if (Ccode.Length >= 8)
                {
                    writer.Write(Convert.ToByte(Ccode.Substring(0, 8), 2));
                    Ccode = Ccode.Remove(0, 8);
                }
            }
            if (Ccode.Length != 0)
            {
                writer.Write(Convert.ToByte(Ccode, 2));
                Ccode = string.Empty;
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
                DecompressLZWTable.Add(code, new List<byte>(text[i + 2]));
                code++;
            }
            var CompressedText = new byte[text.Length - (2 + text[1])];
            for (int i = 0; i < CompressedText.Length; i++)
            {
                CompressedText[i] = text[2 + text[1] + i];
            }
            return CompressedText;
        }

        private List<int> Decompression(byte[] compressedText)
        {
            List<int> Codes = new List<int>();
            string binaryNum = string.Empty;
            foreach (var item in compressedText)
            {
                binaryNum += Convert.ToString(item, 2);
                if (binaryNum.Length >= MaxValueLength)
                {
                    var index = Convert.ToByte(binaryNum.Substring(0, MaxValueLength), 2);
                    binaryNum = binaryNum.Remove(0, MaxValueLength);
                    Codes.Add(index);
                    DecompressValues[0] = DecompressValues[1];
                    DecompressValues[1] = DecompressLZWTable[index];
                    DecompressValues[2].Clear();
                    foreach (var value in DecompressValues[0])
                    {
                        DecompressValues[2].Add(value);
                    }
                    DecompressValues[2].Add(DecompressValues[1][0]);
                    if (!DecompressLZWTable.ContainsValue(DecompressValues[2]))
                    {
                        DecompressLZWTable.Add(code, DecompressValues[2]);
                        code++;
                    }
                }
            }
            return Codes;
        }

        public string DecompressText(string text)
        {
            var buffer = ByteConverter.ConvertToBytes(text);
            MaxValueLength = buffer[0];
            buffer = FillDecompressionDictionary(buffer);
            var DecompressedIndexes = Decompression(buffer);
            var DecompressedText = string.Empty;
            var BytesToWrite = new List<byte>();
            foreach (var index in DecompressedIndexes)
            {
                foreach (var value in DecompressLZWTable[index])
                {
                    BytesToWrite.Add(value);
                }
            }
            return ByteConverter.ConvertToString(BytesToWrite.ToArray());
        }

        public async Task DecompressFile(IFormFile file, string name)
        {

        }
        #endregion
    }
}

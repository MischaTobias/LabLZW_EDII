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
using System.Security.Cryptography;
using System.Net;

namespace CustomCompressors.Compressors
{
    public class LZWCompress : ICompressor
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


        private void ResetVariables()
        {
            LZWTable.Clear();
            DecompressLZWTable.Clear();
            Differentchar.Clear();
            Characters.Clear();
            NumbersToWrite.Clear();
            DecompressValues.Clear();
            MaxValueLength = 0;
            code = 1;
        }
        #endregion

        #region Compression
        private void FillDictionary(byte[] Text)
        {
            //First reading through the text
            foreach (var character in Text)
            {
                if (!LZWTable.ContainsKey(character.ToString()))
                {
                    LZWTable.Add(character.ToString(), code);
                    code++;
                    Differentchar.Add(character);
                }
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
                Subchain = Characters[i].ToString();
                i++;
                while (LZWTable.ContainsKey(Subchain))
                {
                    if (Characters.Count ==1)
                    {
                        NumbersToWrite.Add(LZWTable[Subchain]);
                        Characters = new List<byte>();
                        break;
                    }
                    else
                    {
                        if (Characters.Count - 1 > i - 1)
                        {
                            if (!LZWTable.ContainsKey(Subchain + Characters[i].ToString()))
                            {
                                NumbersToWrite.Add(LZWTable[Subchain]);
                                if (MaxValueLength < LZWTable[Subchain])
                                {
                                    MaxValueLength = LZWTable[Subchain];
                                }
                            }
                            Subchain += Characters[i].ToString();
                            i++;

                        }
                        else
                        {
                            if (!LZWTable.ContainsKey(Subchain + Characters[i].ToString()))
                            {
                                NumbersToWrite.Add(LZWTable[Subchain]);
                                if (MaxValueLength < LZWTable[Subchain])
                                {
                                    MaxValueLength = LZWTable[Subchain];
                                }
                            }
                            else
                            {
                                NumbersToWrite.Add(LZWTable[Subchain + Characters[i].ToString()]);
                            }
                            break;
                        }
                    }
                }

                if (!LZWTable.ContainsKey(Subchain))
                {
                    LZWTable.Add(Subchain, code);
                    code++;
                }

                if (Characters.Count - 1 >= i-1)
                {
                    for (int j = 0; j < i-1; j++)
                    {
                        Characters.RemoveAt(0);
                    }
                }
            }
            MaxValueLength = Convert.ToString(MaxValueLength, 2).Length;
        }
       
        public string CompressText(string text)
        {
            var buffer = ByteConverter.ConvertToBytes(text);//falta repetir esto varias veces por si es un texto muy grande
            FillDictionary(buffer);
            Compression(buffer);
            List<byte> returningList = new List<byte>
            {
                Convert.ToByte(MaxValueLength),
                Convert.ToByte(Differentchar.Count())
            };
            foreach (var item in Differentchar)
            {
                returningList.Add(item);
            }
            string code = string.Empty;
            foreach (var number in NumbersToWrite)
            {
                string subcode = Convert.ToString(number, 2);
                while (subcode.Length != MaxValueLength)
                {
                    subcode = "0" + subcode;
                }
                code += subcode;
                if (code.Length >= 8)
                {
                    returningList.Add(Convert.ToByte(code.Substring(0, 8), 2));
                    code = code.Remove(0, 8);
                }
            }
            if (code.Length != 0)
            {
                while (code.Length != 8)
                {
                    code += "0";
                }
                returningList.Add(Convert.ToByte(code, 2));
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
            ResetVariables();

            using var saver = new FileStream($"{path}/Uploads/{file.FileName}", FileMode.OpenOrCreate);
            await file.CopyToAsync(saver);

            using var reader = new BinaryReader(saver);
            int bufferSize = 2000;
            var buffer = new byte[bufferSize];
            saver.Position = saver.Seek(0, SeekOrigin.Begin);
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

            if (!Directory.Exists($"{path}/Compressions"))
            {
                Directory.CreateDirectory($"{path}/Compressions");
            }
            using var fileToWrite = new FileStream($"{path}/Compressions/{name}.lzw", FileMode.OpenOrCreate);
            using var writer = new BinaryWriter(fileToWrite);
            string compressionCode = "";
            writer.Write(Convert.ToByte(MaxValueLength));
            writer.Write(Convert.ToByte(Differentchar.Count()));
            foreach (var item in Differentchar)
            {
                writer.Write(item);
            }
            string code = "";
            foreach (var number in NumbersToWrite)
            {
                compressionCode = Convert.ToString(number, 2);
                while (compressionCode.Length != MaxValueLength)
                {
                    compressionCode = "0" + compressionCode;
                }
                code += compressionCode;
                while (code.Length >= 8)
                {
                    writer.Write(Convert.ToByte(code.Substring(0, 8), 2));
                    code = code.Remove(0, 8);
                }
            }
            if (code.Length != 0)
            {
                while (code.Length != 8)
                {
                    code += "0";
                }
                writer.Write(Convert.ToByte(code, 2));
                code = string.Empty;
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
                    DecompressLZWTable.Add(code, new List<byte> { text[i + 2] });
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
            DecompressValues.Add(new List<byte>());
            DecompressValues.Add(new List<byte>());
            DecompressValues.Add(new List<byte>());
            foreach (var item in compressedText)
            {
                string subinaryNum = Convert.ToString(item, 2);
                while (subinaryNum.Length < 8)
                {
                    subinaryNum = "0" + subinaryNum;
                }
                binaryNum += subinaryNum;
                while (binaryNum.Length > MaxValueLength)
                {
                    if (binaryNum.Length >= MaxValueLength)
                    {
                        var index = Convert.ToByte(binaryNum.Substring(0, MaxValueLength), 2);
                        if (DecompressLZWTable.Values.Count > 108)
                        {
                            bool flag = true;
                        }
                        binaryNum = binaryNum.Remove(0, MaxValueLength);
                        if (index != 0)
                        {
                            Codes.Add(index);
                            DecompressValues[0] = DecompressValues[1];
                            DecompressValues[1] = DecompressLZWTable[index];
                            DecompressValues[2].Clear();
                            foreach (var value in DecompressValues[0])
                            {
                                DecompressValues[2].Add(value);
                            }
                            DecompressValues[2].Add(DecompressValues[1][0]);
                            if (!CheckIfExists(DecompressValues[2]))
                            {
                                DecompressLZWTable.Add(code, new List<byte>(DecompressValues[2]));
                                code++;
                            }
                        }
                    }
                }
            }
            DecompressValues.Clear();
            return Codes;
        }

        private bool CheckIfExists(List<byte> actualString)
        {
            foreach (var item in DecompressLZWTable.Values)
            {
                if (actualString.Count == item.Count)
                {
                    if (CompareListofBytes(actualString, item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CompareListofBytes(List<byte> list1, List<byte> list2)
        {
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public string DecompressText(string text)
        {
            var buffer = ByteConverter.ConvertToBytes(text);
            MaxValueLength = buffer[0];
            buffer = FillDecompressionDictionary(buffer);
            var DecompressedIndexes = Decompression(buffer);        
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

        public async Task DecompressFile(string path, IFormFile file, string name)
        {
            if (System.IO.File.Exists($"{path}/Uploads/{file.FileName}"))
            {
                System.IO.File.Delete($"{path}/Uploads/{file.FileName}");
            }

            if (System.IO.File.Exists($"{path}/Decompressions/{name}"))
            {
                System.IO.File.Delete($"{path}/Decompressions/{name}");
            }
            ResetVariables();
            using var saver = new FileStream($"{path}/Uploads/{file.FileName}", FileMode.OpenOrCreate);
            await file.CopyToAsync(saver);

            using var reader = new BinaryReader(saver);
            int bufferSize = 2000;
            var buffer = new byte[bufferSize];
            saver.Position = saver.Seek(0, SeekOrigin.Begin);
            buffer = reader.ReadBytes(bufferSize);
            MaxValueLength = buffer[0];
            buffer = FillDecompressionDictionary(buffer);
            var DecompressedIndexes = Decompression(buffer);
            while (saver.Position != saver.Length)
            {
                buffer = reader.ReadBytes(bufferSize);
                foreach (var number in Decompression(buffer))
                {
                    DecompressedIndexes.Add(number);
                }
            }
            reader.Close();
            saver.Close();

            if (!Directory.Exists($"{path}/Decompressions"))
            {
                Directory.CreateDirectory($"{path}/Decompressions");
            }
            using var fileToWrite = new FileStream($"{path}/Decompressions/{name}", FileMode.OpenOrCreate);
            using var writer = new BinaryWriter(fileToWrite);
            foreach (var index in DecompressedIndexes)
            {
                foreach (var value in DecompressLZWTable[index])
                {
                    writer.Write(value);
                }
            }
            writer.Close();
            fileToWrite.Close();
        }
        #endregion
    }
}

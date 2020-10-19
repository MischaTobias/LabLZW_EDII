using CustomCompressors.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CustomCompressors.Utilities;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;

namespace CustomCompressors.Compressors
{
    public class LZWCompress : ICompressor
    {
        #region Variables
        Dictionary<List<byte>, int> ValuesDictionary = new Dictionary<List<byte>, int>();
        //Dictionary<string, int> LZWTable = new Dictionary<string, int>();
        List<byte> Differentchar = new List<byte>();
        List<byte> Characters = new List<byte>();
        int code = 1;
        #endregion
        public byte[] Compression(byte[] Text)
        {
            //Primer recorrido al arreglo
            //string chara = "";
            List<byte> byteList = new List<byte>(Text);
            List<byte> newByteList = new List<byte>();
            while (byteList.Count > 0)
            {
                newByteList.Add(byteList[0]);
                byteList.RemoveAt(0);
                if (!ValuesDictionary.ContainsKey(newByteList))
                {
                    ValuesDictionary.Add(newByteList, code);
                    code++;
                    newByteList = new List<byte>();
                }
            }

            //foreach (var character in Text)
            //{
            //    chara = character.ToString();
            //    if (!LZWTable.ContainsKey(chara))
            //    {
            //        LZWTable.Add(chara, code);
            //        code++;
            //        Differentchar.Add(character);
            //    }
            //}

            //Segundo recorrido y asignación de valores
            Characters = new List<byte>(Text);
            List<byte> Subchain = new List<byte>();
            string Code = "";
            int max = 0;
            List<int> codes = new List<int>();
            while (Characters.Count != 0)
            {
                int i = 0;
                Subchain.Add(Characters[i]);
                Subchain.RemoveAt(i);
                if(Characters.Count > 1)
                {
                    while (ValuesDictionary.ContainsKey(Subchain))
                    {
                        Subchain.Add(Characters[i]);
                        Characters.RemoveAt(i);
                        if (!ValuesDictionary.ContainsKey(Subchain))
                        {
                            codes.Add(ValuesDictionary[Subchain]);
                            if (max < ValuesDictionary[Subchain])
                            {
                                max = ValuesDictionary[Subchain];
                            }
                        }
                        i++;
                        Subchain.Add(Characters[i]);
                    }
                    ValuesDictionary.Add(Subchain, code);
                    code++;
                }
                else
                {
                    codes.Add(ValuesDictionary[Subchain]);
                }
                Characters.RemoveAt(0);
            }

            max = Convert.ToString(max, 2).Length;
            string subcode = "";
            foreach (var number in codes)
            {
                subcode = Convert.ToString(number, 2);
                while (subcode.Length % max !=0)
                {
                    subcode = "0" + subcode;
                }
                Code += subcode;
            }
            while (Code.Length % 8 != 0)
            {
              Code += "0";
            }


            //Construcción del arreglo final
            var Finalcode = new List<byte>();
            Finalcode.Add(Convert.ToByte(max));
            Finalcode.Add(Convert.ToByte(Differentchar.Count()));
            foreach (var item in Differentchar)
            {
                Finalcode.Add(item);
            }
            var stringBytes = (from Match m in Regex.Matches(Code, @"\d{8}") select m.Value).ToList();
            foreach (var byteData in stringBytes)
            {
                Finalcode.Add(Convert.ToByte(byteData, 2));
            }
            return Finalcode.ToArray();
        }
       
        public byte[] Decompression(byte[] CompressedText)
        {
            Dictionary<int, string> LZWTableDC = new Dictionary<int, string>();
            List<byte> Differentchar = new List<byte>();
            List<byte> Characters = new List<byte>();
            code = 1;
            for (int i = 0; i < CompressedText[1]; i++)
            {
                if (!LZWTableDC.ContainsValue(CompressedText[i + 2].ToString()))
                {
                    LZWTableDC.Add(code, CompressedText[i + 2].ToString());
                    code++;
                    Differentchar.Add(CompressedText[i + 2]);
                }
            }
            Characters = CompressedText.ToList<byte>();
            for (int i = 0; i < 2 + CompressedText[1]; i++)
            {
                Characters.RemoveAt(0);
            }
            string Text = "";
            string subtext = "";
            foreach (var code in Characters)
            {
                subtext = Convert.ToString(code, 2);
                while (subtext.Length % 8 != 0)
                {
                    subtext = "0" + subtext;
                }
                Text += subtext;
            }
            
            Characters.Clear();
            if (Text.Length % 3 != 0)
            {
                Text = Text.Substring(0, Text.Length - (Text.Length % 3));
            }
            while (Text.Length > 0)
            {
                Characters.Add(Convert.ToByte(Text.Substring(0,CompressedText[0]),2));
                Text = Text.Remove(0, CompressedText[0]);
            }
            var Finalcode = new List<byte>();
            while (Characters.Count > 0)
            {
                Finalcode.Add(Convert.ToByte(LZWTableDC[Characters[0]]));
                if(Characters.Count > 1)
                {
                    LZWTableDC.Add(code, Convert.ToString(LZWTableDC[Characters[0]] + LZWTableDC[Characters[1]]));
                    code++;
                }
                Characters.RemoveAt(0);
            }
            return Finalcode.ToArray(); 
        }

        public string CompressString(string text)
        {
            int bufferSize = 2000000;
            var buffer = new byte[bufferSize];
            buffer = ByteConverter.ConvertToBytes(text);
            buffer = Compression(buffer);
            return ByteConverter.ConvertToString(buffer);
        }
        
        public string DecompressString(string text)
        {
            int bufferSize = 2000000;
            var buffer = new byte[bufferSize];
            buffer = ByteConverter.ConvertToBytes(text);
            buffer = Decompression(buffer);
            return ByteConverter.ConvertToString(buffer);
        }
    }
}

using CustomCompressors.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CustomCompressors.Utilities;

namespace CustomCompressors.Compressors
{
    public class LZWCompress : ICompressor
    {
        #region Variables
        Dictionary<string, int> LZWTable = new Dictionary<string, int>();
        List<byte> Differentchar = new List<byte>();
        List<byte> Characters = new List<byte>();
        int code = 1;
        #endregion
        public byte[] Compression(byte[] Text)
        {
            //Primer recorrido al arreglo
            string chara = "";
            foreach (var character in Text)
            {
                chara = character.ToString();
                if (!LZWTable.ContainsKey(chara))
                {
                    LZWTable.Add(chara, code);
                    code++;
                    Differentchar.Add(character);
                }
            }

            //Segundo recorrido y asignación de valores
            Characters = Text.ToList<byte>();
            string Subchain;
            string Code = "";
            int max = 0;
            List<int> codes = new List<int>();
            while (Characters.Count != 0)
            {
                int i = 0;
                Subchain = Characters.ElementAt<byte>(i).ToString();
                if(Characters.Count > 1)
                {
                    while (LZWTable.ContainsKey(Subchain))
                    {
                        if (!LZWTable.ContainsKey(Subchain + Characters.ElementAt<byte>(i).ToString()))
                        {
                            codes.Add(LZWTable[Subchain]);
                            if (max < LZWTable[Subchain])
                            {
                                max = LZWTable[Subchain];
                            }
                        }
                        i++;
                        Subchain += Characters.ElementAt<byte>(i).ToString();
                    }
                    LZWTable.Add(Subchain, code);
                    code++;
                }
                else
                {
                    codes.Add(LZWTable[Subchain]);
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
            if (Code.Length != 0)
            {
                while (Code.Length % 8 != 0)
                {
                    Code += "0";
                }
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
        //public byte[] Decompression(byte[] CompressedText)
        //{
        //    Dictionary<string, int> LZWTable = new Dictionary<string, int>();
        //    List<byte> Differentchar = new List<byte>();
        //    List<byte> Characters = new List<byte>();
        //    code = 0;
        //    for (int i = 0; i < CompressedText[1]; i++)
        //    {
        //        if (!LZWTable.ContainsKey(CompressedText[i + 2].ToString()))
        //        {
        //            LZWTable.Add(CompressedText[i + 2].ToString(), code);
        //            code++;
        //            Differentchar.Add(CompressedText[i+2]);
        //        }
        //    }

        //}

        public string CompressString(string text)
        {
            int bufferSize = 2000000;
            var buffer = new byte[bufferSize];
            buffer = ByteConverter.ConvertToBytes(text);
            buffer = Compression(buffer);
            return ByteConverter.ConvertToString(buffer);

        }
    }
}

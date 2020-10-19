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
        Dictionary<List<byte>, int> LZWTable = new Dictionary<List<byte>, int>();
        List<byte> Differentchar = new List<byte>();
        List<byte> Characters = new List<byte>();
        int code = 1;
        #endregion
        public byte[] Compression(byte[] Text)
        {
            //Primer recorrido al arreglo
            var chara = new List<byte>();
            foreach (var character in Text)
            {
                string x = character.ToString();
                chara.Clear();
                chara.Add(character);
                if (!LZWTable.ContainsKey(chara))
                {
                    LZWTable.Add(chara, code);
                    code++;
                    Differentchar.Add(character);
                }
            }

            //Segundo recorrido y asignación de valores
            Characters = Text.ToList<byte>();
            List<byte> Subchain;
            string Code = "";
            int max = 0;
            while (Characters.Count != 0)
            {
                Subchain = new List<byte>();
                int i = 0;
                Subchain.Add(Characters.ElementAt<byte>(i));

                while (LZWTable.ContainsKey(Subchain))
                {
                    i++;
                    Subchain.Add(Characters.ElementAt<byte>(i));
                }
                if (max < LZWTable[Subchain])
                {
                    max = LZWTable[Subchain];
                }
                Code = Convert.ToString(LZWTable[Subchain], 2);
                for (int j = 0; j < i + 1; j++)
                {
                    Characters.RemoveAt(0);
                }
            }
            if (Code.Length != 0)
            {
                while (Code.Length % 8 != 0)
                {
                    Code += "0";
                }
                Convert.ToByte(Code, 2);
            }

            //Construcción del arreglo final
            var Finalcode = new List<byte>();
            Finalcode.Add(Convert.ToByte(Convert.ToString(max, 2).Length));
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
        //    int maxbitlength = CompressedText[0];

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

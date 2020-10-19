using System;
using System.Collections.Generic;
using System.Text;

namespace CustomCompressors.Utilities
{
    class ByteConverter
    {
        static Encoding e = Encoding.GetEncoding("iso-8859-1");
        public static byte[] ConvertToBytes(string text)
        {
            return e.GetBytes(text);
        }

        public static byte ConvertToByte(char character)
        {
            char[] convert = new char[1];
            convert[0] = character;
            var bytes = e.GetBytes(convert);
            return bytes[0];
        }

        public static string ConvertToString(byte[] bytes)
        {
            return e.GetString(bytes);
        }
    }
}

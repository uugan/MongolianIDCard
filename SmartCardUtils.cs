using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonSign
{
    public class SmartCardUtils
    {
        public static bool IsResponseValid(byte[] response)
        {
            if (response.Length < 2)
                return false;

            return response[response.Length - 2] == 0x90 && response[response.Length - 1] == 0x00;
        }
        public static byte[] RemoveLastTwoBytes(byte[] data)
        {
            if (data.Length < 2)
                return new byte[0];

            return data.Take(data.Length - 2).ToArray();
        }

        public static byte[] RemoveFirstTwoBytes(byte[] data)
        {
            if (data.Length < 2)
                return new byte[0];

            return data.Skip(2).ToArray();
        }
        public static string stringToHex(string input)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(input);
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:X2}", b);
            }
            return hex.ToString();
        }
    }
}

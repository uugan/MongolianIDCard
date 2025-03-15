using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonSign
{
    class ApduCommand
    {
        public string Name { get; set; } // Command name
        public string CLA { get; set; }  // Class byte
        public string INS { get; set; }  // Instruction byte
        public string P1 { get; set; }   // Parameter 1
        public string P2 { get; set; }   // Parameter 2
        public string Data { get; set; } // Command Data (optional)
        public string Le { get; set; }   // Expected length of response (optional)

        public byte[] ToByteArray()
        {
            List<byte> command = new List<byte>
        {
            Convert.ToByte(CLA, 16),
            Convert.ToByte(INS, 16),
            Convert.ToByte(P1, 16),
            Convert.ToByte(P2, 16)
        };

            if (!string.IsNullOrEmpty(Data))
            {
                byte[] dataBytes = Enumerable.Range(0, Data.Length / 2)
                                             .Select(i => Convert.ToByte(Data.Substring(i * 2, 2), 16))
                                             .ToArray();
                command.Add((byte)dataBytes.Length);
                command.AddRange(dataBytes);
            }

            if (!string.IsNullOrEmpty(Le))
            {
                command.Add(Convert.ToByte(Le, 16));
            }

            return command.ToArray();
        }

    }
}

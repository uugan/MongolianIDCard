using PCSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonSign
{
    class SmartCardReader : ISmartCardInterface
    {
        private ICardReader _reader;

        public SmartCardReader(ICardReader reader)
        {
            _reader = reader;
        }

        public bool IsCardPresent()
        {
            ReaderStatus status = _reader.GetStatus();
            return status.State == SCardState.Present;
        }

        public byte[] GetAttrib()
        {
            return _reader.GetAttrib(SCardAttribute.AtrString);
        }

        public byte[] SendApdu(ApduCommand command)
        {
            byte[] responseBytes = new byte[0];
            try
            {

                byte[] receiveBuffer = new byte[256];
                byte[] apduBytes = command.ToByteArray();
                int receivedLength = receiveBuffer.Length;
                var response = _reader.Transmit(apduBytes, receiveBuffer);
                responseBytes = receiveBuffer.Take(response).ToArray();

                if (responseBytes.Length >= 2 && (responseBytes[responseBytes.Length - 2] == 0x61 || responseBytes[responseBytes.Length - 2] == 0x6C))
                {
                    byte[] readCommand = new byte[255];
                    byte[] additionalData = Array.Empty<byte>();

                    if (responseBytes[responseBytes.Length - 2] == 0x61 && responseBytes[responseBytes.Length - 1] > 0)
                    {
                        readCommand = new byte[] { 0x00, 0xc0, 0x00, 0x00, responseBytes[responseBytes.Length - 1] };

                        int additionalLength = _reader.Transmit(readCommand, receiveBuffer);
                        additionalData = receiveBuffer.Take(additionalLength).ToArray();

                    }
                    else if (responseBytes[responseBytes.Length - 2] == 0x6C && responseBytes[responseBytes.Length - 1] > 0)
                    {
                        command.Le = responseBytes[responseBytes.Length - 1].ToString("X2");

                        int responseLen = _reader.Transmit(command.ToByteArray(), receiveBuffer);
                        additionalData = receiveBuffer.Take(responseLen).ToArray();

                    }

                    responseBytes = responseBytes.Take(responseBytes.Length - 2).Concat(additionalData).ToArray();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error during APDU transmission:" + ex.Message);
            }
            return responseBytes;


        }

    }
}

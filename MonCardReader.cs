using MonSign.config;
using MonSign.models;
using PCSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonSign
{
    public class MonCardReader
    {
        private List<ApduCommand> apduCommands = new List<ApduCommand>();
        private ICardReader cardReader;
        SmartCardReader smartCardReader;
        public MonCardReader(ICardReader reader)
        {
            cardReader = reader;
            smartCardReader = new SmartCardReader(cardReader);
            LoadApduCommands("UniSign.apdu_commands.json");
        }

        private void LoadApduCommands(string fileName)
        {
            apduCommands = ApduCommandLoader.LoadFromJson(fileName);
        }

        public CitizenInfo readCitizenInfo()
        {

            var selectMFInfoCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_MF");
            var selectDFInfoCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_DF_INFO");
            var selectEfInfoCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_EF_INFO");
            var readBinaryCardInfo = apduCommands.FirstOrDefault(cmd => cmd.Name == "READ_BINARY_CARDINFO");
            var readBinaryAddress = apduCommands.FirstOrDefault(cmd => cmd.Name == "READ_BINARY_ADDRESS");
            var selectEfPhotoCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_EF_PHOTO");
            CitizenInfo citizenInfo = new CitizenInfo();


            byte[] atr = smartCardReader.GetAttrib();
            string atr_str = BitConverter.ToString(atr);
            // Console.WriteLine("ATR:"+ atr_str);

            byte cardtype = 0x01;
            if (atr_str == "3B-7F-96-00-00-80-31-80-65-B0-85-05-00-11-12-0F-FF-82-90-00") // NFC IDCard
            {
                cardtype = 0x02;
                selectDFInfoCommand.P1 = "00";
                selectEfInfoCommand.Data = cardtype.ToString("X2") + selectEfInfoCommand.Data.Substring(2);
                selectEfPhotoCommand.Data = cardtype.ToString("X2") + selectEfPhotoCommand.Data.Substring(2);
            }

            if (selectMFInfoCommand != null)
            {
                byte[] response = smartCardReader.SendApdu(selectMFInfoCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return citizenInfo;
            }
            if (selectDFInfoCommand != null)
            {

                byte[] response = smartCardReader.SendApdu(selectDFInfoCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return citizenInfo;
            }

            if (selectEfInfoCommand != null)
            {

                byte[] response = smartCardReader.SendApdu(selectEfInfoCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return citizenInfo;
            }

            if (readBinaryCardInfo != null)
            {
                byte[] binaryCardInfoResponse = smartCardReader.SendApdu(readBinaryCardInfo);
                if (!SmartCardUtils.IsResponseValid(binaryCardInfoResponse)) return citizenInfo;
                byte[] barrAddress = smartCardReader.SendApdu(readBinaryAddress);
                if (!SmartCardUtils.IsResponseValid(barrAddress)) return citizenInfo;

                binaryCardInfoResponse = binaryCardInfoResponse.Take(binaryCardInfoResponse.Length - 2).ToArray();
                byte[] result = binaryCardInfoResponse.Concat(barrAddress).ToArray();
                citizenInfo = parseCitizenInfo(result);
            }

            if (selectEfPhotoCommand != null)
            {

                byte[] jpeg2000Array = readPortrait(smartCardReader, selectEfPhotoCommand);
                citizenInfo.portrait = jpeg2000Array;
            }

            return citizenInfo;
        }


        private CitizenInfo parseCitizenInfo(byte[] data)
        {
            CitizenInfo citizenInfo = new CitizenInfo();
            int index = 0;


            while (index < data.Length)
            {
                byte tag = data[index++];
                int length = 0;
                if (index + 1 < data.Length)
                    length = (data[index++] << 8) + data[index++];

                switch (tag)
                {
                    case 0x01: // Registration Number
                        citizenInfo.reg_num = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x02: // Birthday
                        citizenInfo.birthday = Encoding.ASCII.GetString(data, index, length);
                        break;
                    case 0x03: // Sex
                        citizenInfo.gender = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x04: // Given Name
                        citizenInfo.givenname = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x05: // Surname Name
                        citizenInfo.surname = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x06: // Family Name
                        citizenInfo.familyname = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x07: // Date of expiry
                        citizenInfo.DOE = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x08: // Date of Issue
                        citizenInfo.DOI = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x09: // Issuer
                        citizenInfo.issuer = Encoding.UTF8.GetString(data, index, length);
                        break;

                    case 0x0A: // Birth place
                        citizenInfo.birth_place = Encoding.UTF8.GetString(data, index, length);
                        break;

                    case 0x0B: // New register
                        citizenInfo.new_reg_num = Encoding.UTF8.GetString(data, index, length);
                        break;

                    case 0x0C: // id card number
                        citizenInfo.id_card_number = Encoding.UTF8.GetString(data, index, length);
                        break;
                    case 0x0D: // address
                        citizenInfo.address = Encoding.UTF8.GetString(data, index, length);
                        break;
                        // Add more cases as needed for other fields
                }

                index += length;

            }

            return citizenInfo;
        }

        private byte[] readPortrait(SmartCardReader smartCardReader, ApduCommand command)
        {
            byte[] jpeg2000Array = new byte[15365];
            try
            {
                byte[] respPortrait = smartCardReader.SendApdu(command);

                using (MemoryStream m = new MemoryStream())
                {

                    for (int i = 0; i <= 60; i++)
                    {
                        byte offset = (byte)((i <= 1) ? 0 : (i - 1));
                        ApduCommand cmd = new ApduCommand()
                        {
                            CLA = "00",
                            INS = "B0",
                            P1 = offset.ToString("X2"),
                            P2 = ((byte)(0x00 - i << 1)).ToString("X2"),
                            Le = "FE"
                        };

                        byte[] respPart = smartCardReader.SendApdu(cmd);
                        m.Write(respPart, 0, respPart.Length - 2);
                    }
                    jpeg2000Array = m.ToArray();
                }
                jpeg2000Array = jpeg2000Array.Skip(5).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:" + ex.Message);
                Console.WriteLine("StackTrace:" + ex.StackTrace);
            }

            return jpeg2000Array;
        }

     
        public byte[] signDoc(string pin, byte[] docHash)
        {
            byte[] signed_data = new byte[0];

            var selectAppCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_SIGN_APPLICATION");
            var verifyPINCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "VERIFY_SIGN_PIN");
            var setAlgorithmCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SET_SIGN_ALGORITHM");

            var signCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SIGN_HASH");
            var getSignedDataCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "GET_SIGNED_DATA");


            if (selectAppCommand != null)
            {
                byte[] response = smartCardReader.SendApdu(selectAppCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return response;
            }

            if (verifyPINCommand != null)
            {
                verifyPINCommand.Data = SmartCardUtils.stringToHex(pin) + "30303030"; // adding 0000
                byte[] response = smartCardReader.SendApdu(verifyPINCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return response;
            }

            if (setAlgorithmCommand != null)
            {
                byte[] response = smartCardReader.SendApdu(setAlgorithmCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return response;
            }

            if (signCommand != null)
            {

                byte[] hash = { 0x90, (byte)docHash.Length };
                byte[] hashData = new byte[hash.Length + docHash.Length];
                Array.Copy(hash, 0, hashData, 0, hash.Length);
                Array.Copy(docHash, 0, hashData, hash.Length, docHash.Length);
                signCommand.Data = BitConverter.ToString(hashData).Replace("-", "");
                byte[] response = smartCardReader.SendApdu(signCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return response;
            }

            if (getSignedDataCommand != null)
            {
                signed_data = smartCardReader.SendApdu(getSignedDataCommand);
                if (!SmartCardUtils.IsResponseValid(signed_data)) return signed_data;

            }
            signed_data = signed_data.Take(signed_data.Length - 2).ToArray();
            return signed_data;
        }

        public string readCertificate()
        {
            string certStr = "";
            var selectAppCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_SIGN_APPLICATION");
            var selectDFCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_CERT_DF_INFO");
            var selectEFCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "SELECT_CERT_EF_INFO");
            var readBinaryCommand = apduCommands.FirstOrDefault(cmd => cmd.Name == "READ_BINARY_CERT");

            if (selectAppCommand != null)
            {
                byte[] response = smartCardReader.SendApdu(selectAppCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return certStr;
            }

            if (selectDFCommand != null)
            {
                byte[] response = smartCardReader.SendApdu(selectDFCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return certStr;
            }

            if (selectEFCommand != null)
            {
                byte[] response = smartCardReader.SendApdu(selectEFCommand);
                if (!SmartCardUtils.IsResponseValid(response)) return certStr;
            }


            if (readBinaryCommand != null)
            {

                List<byte> combinedResponse = new List<byte>();

                ApduCommand command = new ApduCommand
                {
                    CLA = "00",
                    INS = "B0",
                    P1 = "00",
                    P2 = "00",
                    Le = "FE"
                };
                byte[] response = smartCardReader.SendApdu(command);
                response = SmartCardUtils.RemoveFirstTwoBytes(response);
                byte[] trimmedResponse = SmartCardUtils.RemoveLastTwoBytes(response);
                combinedResponse.AddRange(trimmedResponse);

                for (int i = 0; i <= 6; i++)
                {
                    command = new ApduCommand
                    {
                        CLA = "00",
                        INS = "B0",
                        P1 = i.ToString("X2"),
                        P2 = (0xFE - (i * 2)).ToString("X2"),
                        Le = "FE"
                    };
                    response = smartCardReader.SendApdu(command);
                    trimmedResponse = SmartCardUtils.RemoveLastTwoBytes(response);
                    combinedResponse.AddRange(trimmedResponse);
                }
                byte[] finalResponse = combinedResponse.ToArray();
                certStr = Encoding.UTF8.GetString(finalResponse);
            }

            return certStr;

        }

      

    }
}

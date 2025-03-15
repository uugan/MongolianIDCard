# MonSign library
This library is for reading Mongolian ID card and signing data with it.
Developed with .net 8.0
Add System.Security.Permission in your project first.

## Add security permission
```
dotnet add package System.Security.Permissions
```
## How to use

In this example for converting portrait image of jpeg2000, you need to install ImageMagick. 

```C#
using UniSign;
using UniSign.Models;
static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {

            Console.WriteLine("Үнэмлэхээ уншуулна уу!");

            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                var readerNames = context.GetReaders();

                if (readerNames.Length == 0)
                {
                    Console.WriteLine("No smart card readers found.");
                    return;
                }

                string readerName = readerNames[0];
                Console.WriteLine("Using reader:"+ readerName);

                ICardReader reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.T0 | SCardProtocol.T1);
                Console.WriteLine("Connected to the card.");

                MonCardReader monCardReader = new MonCardReader(reader);
            
                //FOR READING IDCARD INFO WITH PORTRAIT
                CitizenInfo citizen = monCardReader.readCitizenInfo();
                Console.WriteLine("Citizen Info: " + JsonConvert.SerializeObject(citizen));
                byte[] imageBytes = citizen.portrait;
                var image = new MagickImage(imageBytes);
                image.Format = MagickFormat.Jpeg;
                image.Write("portrait.jpg");

               //FOR GETTING SIGNED DATA
               //use this signed data for PDF signing (maybe by iText library)
               //ex.:
               //document_hash:314B301806092A864886F70D010903310B06092A864886F70D010701302F06092A864886F70D010904312204207CCBEA1FCBA6E1F76E4F8E9D4BCA3AB6F945261591EFED4A59D932ECC8EA80ED
               //hex to hex SHA256
               //hashData:B6CDADDE514F64A9D18C6E3DD635D089314026D59D48FC8DDEB04788068704DE

               //HERE YOU NEED TO GENERATE HASH DATA FROM DOCUMENT AND AGAIN HASH IT BY SHA-256:
                String pin = "0000"; // user's pin code
                byte[] signed_data = monCardReader.signDoc(pin, new byte[]{ 0xB6, 0xCD, 0xAD, 0xDE, 0x51, 0x4F, 0x64, 0xA9, 0xD1, 0x8C, 0x6E, 0x3D, 0xD6, 0x35,
                       0xD0, 0x89 , 0x31 , 0x40 , 0x26 , 0xD5 , 0x9D , 0x48 , 0xFC , 0x8D , 0xDE , 0xB0 , 0x47 , 0x88 , 0x06 , 0x87 , 0x04 , 0xDE });
        
            }
            } catch (Exception ex)
            {
               Console.WriteLine("General error:"+ex.Message);
            }
     }

```
# MongolianIDCard Introduction
Монголын иргэний үнэмлэхний санах ой(чип) унших 

https://burtgel.gov.mn/g-sign - Иргэний үнэмлэхний санах ойн бүтцийг эндээс татаад харж болно

https://www.eftlab.com/knowledge-base/complete-list-of-apdu-responses - Алдааны код тайлбар

## ATRs
ATR гэдэг нь Answer To Reset гэсэн үгний товчлол бөгөөд энэ нь карт уншигч төхөөрөмжийн чипийг холбогдсны дараа картаас илгээгддэг мессеж юм. ATR мессеж нь картын төлөв, шинж чанар, болон харилцааны параметрийн талаархи мэдээллийг агуулдаг.
| ATR  | IdCard төрөл |
| ------------- | ------------- |
| 3B 7A 94 00 00 80 65 A2 01 01 01 3D 72 D6 41  | IdCard old  |
| 3B 7F 96 00 00 80 31 80 65 B0 85 04 01 20 12 0F FF 82  | IdCard new  |
| 3B 7F 96 00 00 80 31 80 65 B0 85 05 00 11 12 0F FF 82  | IdCard new with NFC  |


Энэ төрлөөс хамаарч мэдээлэл татах APDU командын EF ID өөрчлөгдөнө.

## Үнэмлэхний үндсэн мэдээлэл унших
```
00 A4 00 00 02 3F 00    # SELECT MF
00 A4 01 00 02 DF 01    # SELECT EF by ID
00 A4 02 00 02 01 01    # SELECT EF INFO (01 01)
00 B0 00 00 FE          # READ BINARY INFO
00 B0 00 FE FE          # READ BINARY ADDRESS
```

SELECT EF INFO үед хэрэв IdCard old болон IdCard new тохиолдолд ID-н эхний байт нь 0x01 харин NFC үед 0x02 байна:
```
...
00 A4 02 00 02 02 01 00 # SELECT EF INFO (02 01)
...
```
READ BINARY дээрээс ирсэн датаг UTF-8 string лүү хөрвүүлж харж болно. Бүтцийг дээр өгсөн линкээс орж хараад mapping хийх хэрэгтэй.

## Үнэмлэхний цээж зураг унших
Санах ойн бүтцийн мэдээллээс харвал цээж зурагны нийт хэмжээ 15365 байт байх ба эхний 5 байтыг устгаад **JPEG2000** төрлийн файл болгоод үүний дараа **JPG** форматруу хөрвүүлэн авах юм.
Энэ нь нийт 254 байтаар дуустал нь татаж цуглуулна гэсэн үг. 

INFO татахтай ижлээр NFC-тэй үнэмлэх эсэхээс хамаарч SELECT EF PHOTO команд дээрх ID-ний эхний утгыг 01 эсвэл 02 болгоно.
```
00 A4 00 00 02 3F 00    # SELECT MF
00 A4 01 00 02 DF 01    # SELECT EF by ID (DF 01)
00 A4 02 00 02 01 02    # SELEFT EF PHOTO (01 02)
00 B0 00 00 FE          # READ BINARY PHOTO LOOP 1..61 TIMES
00 B0 00 FE FE
00 B0 01 FC FE
00 B0 02 FA FE
...
00 B0 3B 88 7D          # READ BINARY PHOTO END
```
Үүнийг хялбараар програмчилбал (C# with Magick.net and PCSC):
```csharp

 byte[] jpeg2000Array = new byte[15365];
 using (MemoryStream m = new MemoryStream())
 {
     for (int i = 0; i <= 60; i++)
     {
         byte offset = (byte)((i <= 1) ? 0 : (i - 1));
         byte[] readComm = { 0x00, 0xB0, offset, (byte)(0x00 - i <<1 ), 0xfe };
         byte[] respPart = SendApdu(reader, readComm);
         m.Write(respPart, 0, respPart.Length - 2);
     }
     jpeg2000Array = m.ToArray();
 }
 jpeg2000Array = jpeg2000Array.Skip(5).ToArray();
 var image = new MagickImage(jpeg2000Array);
 image.Format = MagickFormat.Jpeg; //without this line it will create .jp2 file.
 image.Write("output_image_new.jpg");
```


# MonSign library
This library is for reading Mongolian ID card and signing data with it in C#.

Developed with .net 8.0.

Add System.Security.Permission in your project first.

## Add security permission
```
dotnet add package System.Security.Permissions
```
## How to use

In this example for converting portrait image of jpeg2000, you need to install ImageMagick. 

```C#
using MonSign;
using MonSign.Models;
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
                    Console.WriteLine("Карт уншигч олдсонгүй.");
                    return;
                }

                string readerName = readerNames[0]; //эхний уншигчийг сонгоно
                Console.WriteLine("Уншигчийн нэр:"+ readerName);

                ICardReader reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.T0 | SCardProtocol.T1);
                Console.WriteLine("Уншигчтай холбогдлоо.");

                MonCardReader monCardReader = new MonCardReader(reader);
            
                //Доорх функц үнэмлэхний зурагтай нь хамт уншина
                CitizenInfo citizen = monCardReader.readCitizenInfo();
                Console.WriteLine("Citizen Info: " + JsonConvert.SerializeObject(citizen));
                byte[] imageBytes = citizen.portrait;
                var image = new MagickImage(imageBytes);
                image.Format = MagickFormat.Jpeg;
                image.Write("portrait.jpg");

               //Цахим тоон гарын үсгээр дамжуулж PDF файлын хэшийг sign хийх үйлдэл
               //sign хийгдсэн датаг ашиглаад iText мэтийн сангаар дамжуулж pdf дээрээ оруулж өгнө
               
               //document_hash:314B301806092A864886F70D010903310B06092A864886F70D010701302F06092A864886F70D010904312204207CCBEA1FCBA6E1F76E4F8E9D4BCA3AB6F945261591EFED4A59D932ECC8EA80ED
               //hex to hex SHA256
               //hashData:B6CDADDE514F64A9D18C6E3DD635D089314026D59D48FC8DDEB04788068704DE

               //Буюу Gsign програмын үйлдлийг харвал 2 удаа хэшлээд байгаа юм
                String pin = "0000"; // үнэмлэх дээр суулгуулсан тоон гарын үсэгний пин код
                byte[] signed_data = monCardReader.signDoc(pin, new byte[]{ 0xB6, 0xCD, 0xAD, 0xDE, 0x51, 0x4F, 0x64, 0xA9, 0xD1, 0x8C, 0x6E, 0x3D, 0xD6, 0x35,
                       0xD0, 0x89 , 0x31 , 0x40 , 0x26 , 0xD5 , 0x9D , 0x48 , 0xFC , 0x8D , 0xDE , 0xB0 , 0x47 , 0x88 , 0x06 , 0x87 , 0x04 , 0xDE });
        
            }
            } catch (Exception ex)
            {
               Console.WriteLine("General error:"+ex.Message);
            }
     }

```

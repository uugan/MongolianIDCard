# MongolianIDCard
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
00 A4 04 00 02 49 44 00 # SELECT EF by ID
00 A4 02 00 02 01 01 00 # SELECT EF INFO (01 01)
00 B0 00 08 FE          # READ BINARY INFO
00 B0 01 07 FE          # READ BINARY ADDRESS
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
00 A4 04 00 02 49 44 00 # SELECT EF by ID (49 44)
00 A4 02 00 02 01 02 00 # SELEFT EF PHOTO (01 02)
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
         byte offset = (i <= 1) ? 0 : (byte)(i - 1);
         byte[] readComm = { 0x00, 0xB0, offset, (byte)(0x00 - i * 2), 0xfe };
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



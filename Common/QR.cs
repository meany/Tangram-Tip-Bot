using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace dm.TanTipBot
{
    public class QR
    {
        public static void Generate(string address, System.Drawing.Color dark, System.Drawing.Color light, ref Stream stream)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(address, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            var qr = qrCode.GetGraphic(12, ColorTranslator.ToHtml(dark), ColorTranslator.ToHtml(light));
            qr.Save(stream, ImageFormat.Png);
        }
    }
}

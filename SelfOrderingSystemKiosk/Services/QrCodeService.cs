using QRCoder;

namespace SelfOrderingSystemKiosk.Services
{
    public class QrCodeService
    {
        public byte[] GetPngBytes(string payload, int pixelsPerModule = 12)
        {
            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule);
        }
    }
}

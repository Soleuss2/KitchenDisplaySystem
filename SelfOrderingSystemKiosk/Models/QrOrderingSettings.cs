namespace SelfOrderingSystemKiosk.Models
{
    /// <summary>Optional public site URL for QR payloads (e.g. https://orders.example.com). If empty, current request host is used.</summary>
    public class QrOrderingSettings
    {
        public string PublicSiteUrl { get; set; } = "";
    }
}

namespace ArtNetSharp
{
    public class ArtNetConfig
    {
        public string Host { get; set; } = "255.255.255.255";
        public int Port { get; set; } = 6454;
        public int Refresh { get; set; } = 4000;
        public bool SendAll { get; set; } = false;
        public string? Iface { get; set; }
    }
}

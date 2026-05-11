using System;

namespace ArtNetSharp
{
    /// <summary>
    /// Builds Art-Net wire packets.
    /// See http://www.artisticlicence.com/WebSiteMaster/User%20Guides/art-net.pdf
    /// </summary>
    internal static class ArtNetPacket
    {
        public const int DmxChannelCount = 512;

        // "Art-Net\0"
        private static readonly byte[] Id = { 65, 114, 116, 45, 78, 101, 116, 0 };

        /// <summary>OpTrigger packet (page 40). Payload is 512 bytes, manufacturer specific.</summary>
        public static byte[] BuildTrigger(int oem, int key, int subkey)
        {
            byte hOem = (byte)((oem >> 8) & 0xff);
            byte lOem = (byte)(oem & 0xff);

            byte[] header =
            {
                Id[0], Id[1], Id[2], Id[3], Id[4], Id[5], Id[6], Id[7],
                0, 0x99, 0, 14, 0, 0, hOem, lOem,
                (byte)(key & 0xff), (byte)(subkey & 0xff)
            };

            byte[] buf = new byte[header.Length + DmxChannelCount];
            Buffer.BlockCopy(header, 0, buf, 0, header.Length);
            return buf;
        }

        /// <summary>OpDmx packet (page 45). Length is forced to even, in [2, 512].</summary>
        public static byte[] BuildArtDmx(int universe, byte[] data, int length)
        {
            if (length < 2) length = 2;
            if ((length & 1) == 1) length += 1;
            if (length > DmxChannelCount) length = DmxChannelCount;

            byte hUni = (byte)((universe >> 8) & 0xff);
            byte lUni = (byte)(universe & 0xff);
            byte hLen = (byte)((length >> 8) & 0xff);
            byte lLen = (byte)(length & 0xff);

            byte[] header =
            {
                Id[0], Id[1], Id[2], Id[3], Id[4], Id[5], Id[6], Id[7],
                0, 0x50, 0, 14, 0, 0, lUni, hUni, hLen, lLen
            };

            byte[] buf = new byte[header.Length + length];
            Buffer.BlockCopy(header, 0, buf, 0, header.Length);
            Buffer.BlockCopy(data, 0, buf, header.Length, length);
            return buf;
        }
    }
}

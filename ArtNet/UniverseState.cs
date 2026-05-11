using System;
using System.Threading;

namespace ArtNetSharp
{
    /// <summary>
    /// Per-universe DMX buffer + refresh/throttle timers.
    /// </summary>
    internal sealed class UniverseState : IDisposable
    {
        public byte[] Data { get; } = new byte[ArtNetPacket.DmxChannelCount];

        /// <summary>Highest 1-based channel number that changed since the last send.</summary>
        public int DataChanged { get; set; }

        /// <summary>Periodic full-frame refresh timer.</summary>
        public Timer? RefreshTimer { get; set; }

        /// <summary>25ms send-throttle timer (null = no send in flight).</summary>
        public Timer? ThrottleTimer { get; set; }

        /// <summary>True if a send was requested while throttled.</summary>
        public bool SendDelayed { get; set; }

        public void Dispose()
        {
            RefreshTimer?.Dispose();
            ThrottleTimer?.Dispose();
        }
    }
}

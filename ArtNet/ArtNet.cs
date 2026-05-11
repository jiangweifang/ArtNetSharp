using System;
using System.Collections.Generic;
using System.Threading;

namespace ArtNetSharp
{
    /// <summary>
    /// C# port of https://github.com/hobbyquaker/artnet (lib/artnet.js).
    /// Sends Art-Net DMX packets over UDP.
    /// </summary>
    public class ArtNet : IDisposable
    {
        private const int ThrottleMilliseconds = 25;

        private readonly object _sync = new();
        private readonly int _refresh;
        private readonly bool _sendAll;
        private readonly ArtNetUdpTransport _transport;
        private readonly Dictionary<int, UniverseState> _universes = new();

        public event EventHandler<ArtNetErrorEventArgs>? Error;

        public ArtNet(ArtNetConfig? config = null)
        {
            config ??= new ArtNetConfig();
            _refresh = config.Refresh;
            _sendAll = config.SendAll;
            _transport = new ArtNetUdpTransport(config);
        }

        /// <summary>Read-only view of the DMX buffers per universe.</summary>
        public IReadOnlyDictionary<int, byte[]> Data
        {
            get
            {
                lock (_sync)
                {
                    var snap = new Dictionary<int, byte[]>(_universes.Count);
                    foreach (var kv in _universes) snap[kv.Key] = kv.Value.Data;
                    return snap;
                }
            }
        }

        // ---- Public API ----------------------------------------------------

        public bool Set(int universe, int channel, byte value, Action<Exception?>? callback = null)
        {
            int idx = channel - 1;
            if (idx < 0 || idx >= ArtNetPacket.DmxChannelCount) return false;

            lock (_sync)
            {
                var u = GetOrCreate(universe);
                if (u.Data[idx] != value)
                {
                    u.Data[idx] = value;
                    if (channel > u.DataChanged) u.DataChanged = channel;
                }
                FlushOrCallback(universe, u, callback);
                return true;
            }
        }

        public bool Set(int universe, int channel, byte[] values, Action<Exception?>? callback = null)
        {
            if (values == null) return false;
            lock (_sync)
            {
                var u = GetOrCreate(universe);
                for (int i = 0; i < values.Length; i++)
                {
                    int index = channel + i - 1;
                    if (index < 0 || index >= ArtNetPacket.DmxChannelCount) break;
                    if (u.Data[index] != values[i])
                    {
                        u.Data[index] = values[i];
                        if ((index + 1) > u.DataChanged) u.DataChanged = index + 1;
                    }
                }
                FlushOrCallback(universe, u, callback);
                return true;
            }
        }

        public bool Set(int channel, byte value, Action<Exception?>? callback = null) => Set(0, channel, value, callback);
        public bool Set(int channel, byte[] values, Action<Exception?>? callback = null) => Set(0, channel, values, callback);
        public bool Set(byte value, Action<Exception?>? callback = null) => Set(0, 1, value, callback);
        public bool Set(byte[] values, Action<Exception?>? callback = null) => Set(0, 1, values, callback);

        /// <summary>
        /// If <paramref name="refresh"/> is true all 512 channels are sent,
        /// otherwise channels 1..lastChanged.
        /// </summary>
        public void Send(int universe, bool refresh = false, Action<Exception?>? callback = null)
        {
            lock (_sync)
            {
                if (_sendAll) refresh = true;

                var u = GetOrCreate(universe);
                EnsureRefreshTimer(universe, u);

                if (u.ThrottleTimer != null)
                {
                    u.SendDelayed = true;
                    return;
                }

                u.ThrottleTimer = new Timer(_ => OnThrottleElapsed(universe, callback), null, ThrottleMilliseconds, Timeout.Infinite);

                int len = refresh ? ArtNetPacket.DmxChannelCount : u.DataChanged;
                var buf = ArtNetPacket.BuildArtDmx(universe, u.Data, len);
                u.DataChanged = 0;
                SendRaw(buf, callback);
            }
        }

        /// <summary>Send an Art-Net trigger. Most devices respond to oem 0xFFFF (broadcast trigger).</summary>
        public bool Trigger(int oem = 0xFFFF, int subkey = 0, int key = 255, Action<Exception?>? callback = null)
        {
            SendTrigger(oem, key, subkey, callback);
            return true;
        }

        /// <summary>Triggers are never throttled.</summary>
        public void SendTrigger(int oem, int key, int subkey, Action<Exception?>? callback = null)
        {
            SendRaw(ArtNetPacket.BuildTrigger(oem, key, subkey), callback);
        }

        public void SetHost(string host) => _transport.SetHost(host);
        public void SetPort(int port) => _transport.SetPort(port);

        public void Close() => Dispose();

        public void Dispose()
        {
            lock (_sync)
            {
                foreach (var u in _universes.Values) u.Dispose();
                _universes.Clear();
                _transport.Dispose();
            }
        }

        // ---- Internals -----------------------------------------------------

        private UniverseState GetOrCreate(int universe)
        {
            if (!_universes.TryGetValue(universe, out var u))
            {
                u = new UniverseState();
                _universes[universe] = u;
            }
            return u;
        }

        private void EnsureRefreshTimer(int universe, UniverseState u)
        {
            if (u.RefreshTimer == null)
            {
                u.RefreshTimer = new Timer(_ => Send(universe, refresh: true), null, _refresh, _refresh);
            }
        }

        private void FlushOrCallback(int universe, UniverseState u, Action<Exception?>? callback)
        {
            if (u.DataChanged > 0) Send(universe, false, callback);
            else callback?.Invoke(null);
        }

        private void OnThrottleElapsed(int universe, Action<Exception?>? callback)
        {
            lock (_sync)
            {
                if (!_universes.TryGetValue(universe, out var u)) return;
                u.ThrottleTimer?.Dispose();
                u.ThrottleTimer = null;
                if (u.SendDelayed)
                {
                    u.SendDelayed = false;
                    Send(universe, false, callback);
                }
            }
        }

        private void SendRaw(byte[] buf, Action<Exception?>? callback)
        {
            try
            {
                _transport.Send(buf);
                callback?.Invoke(null);
            }
            catch (Exception ex)
            {
                callback?.Invoke(ex);
                Error?.Invoke(this, new ArtNetErrorEventArgs(ex));
            }
        }
    }
}

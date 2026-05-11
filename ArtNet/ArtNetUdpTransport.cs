using System;
using System.Net;
using System.Net.Sockets;

namespace ArtNetSharp
{
    /// <summary>
    /// Wraps a UDP socket configured for Art-Net (broadcast, reuse-address, optional bind to iface).
    /// </summary>
    internal sealed class ArtNetUdpTransport : IDisposable
    {
        private readonly object _sync = new();
        private readonly UdpClient _socket;
        private string _host;
        private int _port;

        public ArtNetUdpTransport(ArtNetConfig config)
        {
            _host = config.Host;
            _port = config.Port;

            _socket = new UdpClient(AddressFamily.InterNetwork);
            _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (!string.IsNullOrEmpty(config.Iface) && _host == "255.255.255.255")
            {
                _socket.Client.Bind(new IPEndPoint(IPAddress.Parse(config.Iface), _port));
            }
            else if (_host.EndsWith("255", StringComparison.Ordinal))
            {
                _socket.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            }

            try { _socket.EnableBroadcast = true; } catch { /* ignore */ }
        }

        public void Send(byte[] buffer)
        {
            IPEndPoint ep;
            lock (_sync) ep = new IPEndPoint(IPAddress.Parse(_host), _port);
            _socket.Send(buffer, buffer.Length, ep);
        }

        public void SetHost(string host)
        {
            lock (_sync) _host = host;
        }

        public void SetPort(int port)
        {
            lock (_sync)
            {
                if (_host == "255.255.255.255")
                    throw new InvalidOperationException("Can't change port when using broadcast address 255.255.255.255");
                _port = port;
            }
        }

        public void Dispose()
        {
            try { _socket.Close(); } catch { }
            _socket.Dispose();
        }
    }
}

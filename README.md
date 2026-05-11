# ArtNetSharp

[![License][mit-badge]][mit-url]

> A C# / .NET 10 library for sending [Art-Net](http://en.wikipedia.org/wiki/Art-Net) DMX packets over UDP.
>
> Ported from the Node.js module [hobbyquaker/artnet](https://github.com/hobbyquaker/artnet).

## Requirements

- .NET 10 SDK or later

## Usage

Connect, set channel 1 to 255, then close.

```csharp
using ArtNetSharp;

var artnet = new ArtNet(new ArtNetConfig
{
    Host = "172.16.23.15"
});

// set channel 1 to 255
artnet.Set(1, (byte)255, err =>
{
    artnet.Close();
});
```

The `Set` method can update multiple channels at once:

```csharp
// set channel 100 to 10, channel 101 to 20 and channel 102 to 30
artnet.Set(100, new byte[] { 10, 20, 30 });
```

Omit the channel and it defaults to `1`:

```csharp
// Set channel 1 to 255 and channel 2 to 127
artnet.Set(new byte[] { 255, 127 });
```

You can also send trigger macros to devices:

```csharp
// Send key 3, subkey 1 to all devices
artnet.Trigger(subkey: 1, key: 3);

// Send key 2, subkey 71 (the letter 'G') to ArtNet devices responding to oem 0x6A6B
artnet.Trigger(oem: 0x6A6B, subkey: 71, key: 2);
```

This library throttles the maximum send rate to ~40 Hz. Unchanged data is refreshed every ~4 s.

## Configuration (`ArtNetConfig`)

| Property  | Default               | Description                                                          |
| --------- | --------------------- | -------------------------------------------------------------------- |
| `Host`    | `"255.255.255.255"`   | Target host or broadcast address.                                    |
| `Port`    | `6454`                | UDP port.                                                            |
| `Refresh` | `4000`                | Millisecond interval for resending unchanged DMX data.               |
| `SendAll` | `false`               | If `true`, always sends the full DMX universe instead of just diffs. |
| `Iface`   | `null`                | Optional local IP address to bind the UDP socket to.                 |

## API

#### `bool Set([int universe,] [int channel,] byte value, Action<Exception?>? callback = null)`
#### `bool Set([int universe,] [int channel,] byte[] values, Action<Exception?>? callback = null)`

Every parameter except the value(s) is optional. If you supply a `universe` you must also supply a `channel`.
Defaults: `universe = 0`, `channel = 1`.

The callback is invoked with `null` on success or an `Exception` on failure.
If no data actually changed, nothing is sent.

#### `bool Trigger(int oem = 0xFFFF, int subkey = 0, int key = 255, Action<Exception?>? callback = null)`

Sends an Art-Net `ArtTrigger` packet. Triggers are device-specific and typically used to start/stop shows.

Triggers are **never throttled** and are sent immediately.

#### `void SetHost(string host)`

Change the Art-Net hostname/address after initialization.

#### `void SetPort(int port)`

Change the Art-Net port after initialization.
Does not work when using the broadcast address `255.255.255.255`.

#### `void Close()` / `void Dispose()`

Closes the UDP socket and stops all send/refresh timers.

## Events

- `event EventHandler<ArtNetErrorEventArgs> Error` — raised when a UDP send fails.

## Further Reading

- [Art-Net protocol specification](http://www.artisticlicence.com/WebSiteMaster/User%20Guides/art-net.pdf)

## License

MIT — see [LICENSE](LICENSE).

## Credits

- Original Node.js implementation: [hobbyquaker/artnet](https://github.com/hobbyquaker/artnet) (c) Sebastian Raff and contributors.
- Art-Net designed by and copyright [Artistic Licence Holdings Ltd](http://www.artisticlicence.com/).

[mit-badge]: https://img.shields.io/badge/License-MIT-blue.svg?style=flat
[mit-url]: LICENSE

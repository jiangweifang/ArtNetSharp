using System;

namespace ArtNetSharp
{
    public class ArtNetErrorEventArgs
    {
        public Exception Error { get; }
        public ArtNetErrorEventArgs(Exception error) { Error = error; }
    }
}

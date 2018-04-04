using Inedo.Diagnostics;

namespace Inedo.Extensions.Linux
{
    internal sealed class NullLogSink : ILogSink
    {
        public static NullLogSink Instance { get; } = new NullLogSink();

        public void Log(IMessage message)
        {
            // discard
        }
    }
}

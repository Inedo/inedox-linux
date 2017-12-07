using Inedo.Diagnostics;

namespace Inedo.Extensions.Linux
{
    internal static class LoggerShim
    {
#if BuildMaster || Otter
        public static ILogSink ToLogSink(this ILogger logger) => new Logger(logger);

        private sealed class Logger : ILogSink
        {
            private readonly ILogger logger;

            public Logger(ILogger logger) => this.logger = logger;

            public void Log(IMessage message) => this.logger.Log(message.Level, message.Message);
        }
#else
        public static ILogSink ToLogSink(this ILogSink logger) => logger;
#endif
    }
}

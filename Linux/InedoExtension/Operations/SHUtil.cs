using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.Extensions.Linux.Operations
{
    internal static class SHUtil
    {
        private const int Octal755 = 493;

        public static Task<int?> ExecuteScriptAsync(IOperationExecutionContext context, TextReader scriptReader, string arguments, ILogSink logger, bool verbose, MessageLevel outputLevel = MessageLevel.Information, MessageLevel errorLevel = MessageLevel.Error)
        {
            return ExecuteScriptAsync(context, scriptReader, arguments, logger, verbose, data => LogMessage(outputLevel, data, logger), errorLevel);
        }

        public static async Task<int?> ExecuteScriptAsync(IOperationExecutionContext context, TextReader scriptReader, string arguments, ILogSink logger, bool verbose, Action<string> output, MessageLevel errorLevel = MessageLevel.Error)
        {
            var fileOps = context.Agent.TryGetService<ILinuxFileOperationsExecuter>();
            if (fileOps == null)
            {
                logger.LogError("This operation is only valid when run against an SSH agent.");
                return null;
            }

            var scriptsDirectory = fileOps.CombinePath(fileOps.GetBaseWorkingDirectory(), "scripts");
            await fileOps.CreateDirectoryAsync(scriptsDirectory).ConfigureAwait(false);

            var fileName = fileOps.CombinePath(scriptsDirectory, Guid.NewGuid().ToString("N"));
            try
            {
                if (verbose)
                    logger.LogDebug($"Writing script to temporary file at {fileName}...");

                using (var scriptStream = await fileOps.OpenFileAsync(fileName, FileMode.Create, FileAccess.Write, Octal755).ConfigureAwait(false))
                using (var scriptWriter = new StreamWriter(scriptStream, InedoLib.UTF8Encoding) { NewLine = "\n" })
                {
                    var line = await scriptReader.ReadLineAsync().ConfigureAwait(false);
                    while (line != null)
                    {
                        await scriptWriter.WriteLineAsync(line).ConfigureAwait(false);
                        line = await scriptReader.ReadLineAsync().ConfigureAwait(false);
                    }
                }

                if (verbose)
                {
                    logger.LogDebug("Script written successfully.");
                    logger.LogDebug($"Ensuring that working directory ({context.WorkingDirectory}) exists...");
                }

                await fileOps.CreateDirectoryAsync(context.WorkingDirectory).ConfigureAwait(false);

                if (verbose)
                {
                    logger.LogDebug("Working directory is present.");
                    logger.LogDebug("Script file: " + fileName);
                    logger.LogDebug("Arguments: " + arguments);
                    logger.LogDebug("Executing script...");
                }

                var ps = context.Agent.GetService<IRemoteProcessExecuter>();
                int? exitCode;

                using (var process = ps.CreateProcess(new RemoteProcessStartInfo { FileName = fileName, WorkingDirectory = context.WorkingDirectory, Arguments = arguments }))
                {
                    process.OutputDataReceived += (s, e) => output(e.Data);
                    process.ErrorDataReceived += (s, e) => LogMessage(errorLevel, e.Data, logger);
                    process.Start();
                    await process.WaitAsync(context.CancellationToken).ConfigureAwait(false);
                    exitCode = process.ExitCode;
                }

                if (verbose)
                    logger.LogDebug("Script completed.");

                return exitCode;
            }
            finally
            {
                if (verbose)
                    logger.LogDebug($"Deleting temporary script file ({fileName})...");

                try
                {
                    fileOps.DeleteFile(fileName);
                    if (verbose)
                        logger.LogDebug("Temporary file deleted.");
                }
                catch (Exception ex)
                {
                    if (verbose)
                        logger.LogDebug("Unable to delete temporary file: " + ex.Message);
                }
            }
        }

        private static void LogMessage(MessageLevel level, string text, ILogSink logger)
        {
            if (!string.IsNullOrWhiteSpace(text))
                logger.Log(level, text);
        }

        public static async Task<TextReader> OpenScriptAssetAsync(string name, ILogSink logger, IOperationExecutionContext context)
        {
            var qualifiedName = QualifiedName.Parse(name);
            var scriptName = qualifiedName.Name;
            var raftName = qualifiedName.Namespace ?? RaftRepository.DefaultName;

            var raft = RaftRepository.OpenRaft(raftName);
            try
            {
                if (!scriptName.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
                    scriptName += ".sh";

                if (await raft.GetRaftItemAsync(RaftItemType.Script, scriptName) == null)
                {
                    logger.LogError($"Could not find script {scriptName} in {raftName} raft.");
                    raft.Dispose();
                    return null;
                }

                return new StreamReader(new RaftStream(await raft.OpenRaftItemAsync(RaftItemType.Script, scriptName, FileMode.Open, FileAccess.Read), raft), InedoLib.UTF8Encoding);
            }
            catch
            {
                try { raft?.Dispose(); }
                catch { }
                throw;
            }
        }

        private sealed class RaftStream : Stream
        {
            private Stream baseStream;
            private RaftRepository raft;
            private bool disposed;

            public RaftStream(Stream baseStream, RaftRepository raft)
            {
                this.baseStream = baseStream;
                this.raft = raft;
            }

            public override bool CanRead => this.baseStream.CanRead;
            public override bool CanSeek => this.baseStream.CanSeek;
            public override bool CanWrite => this.baseStream.CanWrite;
            public override long Length => this.baseStream.Length;
            public override long Position
            {
                get => this.baseStream.Position;
                set => this.baseStream.Position = value;
            }

            public override void Flush() => this.baseStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => this.baseStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => this.baseStream.Seek(offset, origin);
            public override void SetLength(long value) => this.baseStream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => this.baseStream.Write(buffer, offset, count);
            public override int ReadByte() => this.baseStream.ReadByte();
            public override void WriteByte(byte value) => this.baseStream.WriteByte(value);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => this.baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => this.baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => this.baseStream.CopyToAsync(destination, bufferSize, cancellationToken);

            protected override void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        this.baseStream.Dispose();
                        this.raft.Dispose();
                    }

                    this.disposed = true;
                }

                base.Dispose(disposing);
            }
        }
    }
}

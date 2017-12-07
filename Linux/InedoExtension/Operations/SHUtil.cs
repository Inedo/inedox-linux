using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.Extensions.Linux.Operations
{
    partial class SHUtil
    {
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

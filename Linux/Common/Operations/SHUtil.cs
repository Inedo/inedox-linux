﻿using System;
using System.IO;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
#if Otter
using Inedo.Otter.Extensibility.Operations;
#elif BuildMaster
using Inedo.BuildMaster.Extensibility.Operations;
#endif

namespace Inedo.Extensions.Linux.Operations
{
    internal static partial class SHUtil
    {
        private const int Octal755 = 493;

        public static async Task<int?> ExecuteScriptAsync(IOperationExecutionContext context, TextReader scriptReader, string arguments, ILogger logger, bool verbose, Action<string> output = null)
        {
            var fileOps = context.Agent.TryGetService<IFileOperationsExecuter>() as ILinuxFileOperationsExecuter;
            if (fileOps == null)
            {
                logger.LogError("This operation is only valid when run against an SSH agent.");
                return null;
            }

            var scriptsDirectory = fileOps.CombinePath(fileOps.GetBaseWorkingDirectory(), "scripts");
            await fileOps.CreateDirectoryAsync(scriptsDirectory);

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
                    if (output == null)
                        process.OutputDataReceived += (s, e) => LogMessage(MessageLevel.Information, e.Data, logger);
                    else
                        process.OutputDataReceived += (s, e) => output(e.Data);

                    process.ErrorDataReceived += (s, e) => LogMessage(MessageLevel.Error, e.Data, logger);
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

        private static void LogMessage(MessageLevel level, string text, ILogger logger)
        {
            if (!string.IsNullOrWhiteSpace(text))
                logger.Log(level, text);
        }
    }
}
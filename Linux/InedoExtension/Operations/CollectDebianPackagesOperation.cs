using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.Configurations;
using Inedo.Extensions.Linux.Configurations;
using System.Linq;

namespace Inedo.Extensions.Linux.Operations
{
    [DisplayName("Collect Debian Packages")]
    [Description("Collects the names and versions of .deb packages installed on a server.")]
    [ScriptAlias("Collect-DebianPackages")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    public sealed class CollectDebianPackagesOperation : CollectPackagesOperation
    {
        public override string PackageType => "Debian";

        protected async override Task<IEnumerable<PackageConfiguration>> CollectPackagesAsync(IOperationCollectionContext context)
        {

            var remoteExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var packages = new List<PackageConfiguration>();

            using (var process = remoteExecuter.CreateProcess(new RemoteProcessStartInfo
            {
                FileName = "/usr/bin/dpkg",
                Arguments = "--list"
            }))
            {
                process.OutputDataReceived += (s, e) =>
                {
                    // installed packages have ii at the start of the line.
                    if (!e.Data.StartsWith("ii "))
                        return;

                    var parts = e.Data.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    packages.Add(new DebianPackageConfiguration { PackageName = parts[1], PackageVersion = parts[2] });
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    this.LogWarning(e.Data);
                };

                process.Start();
                await process.WaitAsync(context.CancellationToken).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    this.LogError($"\"dpkg --list\" exited with code {process.ExitCode}");
                    return Enumerable.Empty<PackageConfiguration>();
                }
            }

            return packages;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Collect Debian Packages")
            );
        }
    }
}
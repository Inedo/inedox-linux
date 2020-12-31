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
    [DisplayName("Collect RPM Packages")]
    [Description("Collects the names and versions of .rpm packages installed on a server.")]
    [ScriptAlias("Collect-RpmPackages")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    public sealed class CollectRpmPackagesOperation : CollectPackagesOperation
    {
        public override string PackageType => "RPM";

        protected async override Task<IEnumerable<PackageConfiguration>> CollectPackagesAsync(IOperationCollectionContext context)
        {

            var remoteExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var packages = new List<PackageConfiguration>();

            using (var process = remoteExecuter.CreateProcess(new RemoteProcessStartInfo
            {
                FileName = "/usr/bin/rpm",
                Arguments = "-qa"
            }))
            {
                process.OutputDataReceived += (s, e) =>
                {
                    var parts = e.Data.Split(new[] { '-' }, 2);
                    packages.Add(new RpmPackageConfiguration { PackageName = parts[0], PackageVersion = parts[1] });
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    this.LogWarning(e.Data);
                };

                process.Start();
                await process.WaitAsync(context.CancellationToken).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    this.LogError($"\"rpm -qa\" exited with code {process.ExitCode}");
                    return Enumerable.Empty<RpmPackageConfiguration>();
                }
            }

            return packages;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Collect RPM Packages")
            );
        }
    }
}

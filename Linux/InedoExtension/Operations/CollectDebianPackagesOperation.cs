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

namespace Inedo.Extensions.Linux.Operations
{
    [DisplayName("Collect Debian Packages")]
    [Description("Collects the names and versions of .deb packages installed on a server.")]
    [ScriptAlias("Collect-DebianPackages")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    public sealed class CollectDebianPackagesOperation : CollectOperation<DictionaryConfiguration>
    {
        public async override Task<DictionaryConfiguration> CollectConfigAsync(IOperationCollectionContext context)
        {
            var remoteExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var packages = new List<Package>();

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
                    packages.Add(new Package(parts[1], parts[2]));
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
                    return null;
                }
            }

            using (var collect = context.GetServerCollectionContext())
            {
                await collect.ClearAllPackagesAsync("Debian");

                foreach (var package in packages)
                    await collect.CreateOrUpdatePackageAsync("Debian", package.Name, package.Version, null);
            }

            return null;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Collect Debian Packages")
            );
        }

        private struct Package
        {
            public Package(string name, string version)
            {
                this.Name = name;
                this.Version = version;
            }

            public string Name { get; }
            public string Version { get; }
        }
    }
}
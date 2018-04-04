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
    [DisplayName("Collect RPM Packages")]
    [Description("Collects the names and versions of .rpm packages installed on a server.")]
    [ScriptAlias("Collect-RpmPackages")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    public sealed class CollectRpmPackagesOperation : CollectOperation<DictionaryConfiguration>
    {
        public async override Task<DictionaryConfiguration> CollectConfigAsync(IOperationCollectionContext context)
        {
            var remoteExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var packages = new List<Package>();

            using (var process = remoteExecuter.CreateProcess(new RemoteProcessStartInfo
            {
                FileName = "/usr/bin/rpm",
                Arguments = "-qa"
            }))
            {
                process.OutputDataReceived += (s, e) =>
                {
                    var parts = e.Data.Split(new[] { '-' }, 2);
                    packages.Add(new Package(parts[0], parts[1]));
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
                    return null;
                }
            }

            using (var collect = context.GetServerCollectionContext())
            {
                await collect.ClearAllPackagesAsync("RPM");

                foreach (var package in packages)
                    await collect.CreateOrUpdatePackageAsync("RPM", package.Name, package.Version, null);
            }

            return null;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Collect RPM Packages")
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

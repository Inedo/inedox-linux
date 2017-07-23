using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Otter.Data;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensions.Configurations;

namespace Inedo.Extensions.Linux.Operations
{
    [DisplayName("Collect RPM Packages")]
    [Description("Collects the names and versions of .rpm packages installed on a server.")]
    [ScriptAlias("Collect-RpmPackages")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    public sealed class CollectRpmPackagesOperation : CollectOperation<DictionaryConfiguration>
    {
        public async override Task<DictionaryConfiguration> CollectConfigAsync(IOperationExecutionContext context)
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

            using (var db = new DB.Context())
            {
                await db.ServerPackages_DeletePackagesAsync(
                    Server_Id: context.ServerId,
                    PackageType_Name: "RPM"
                ).ConfigureAwait(false);

                foreach (var package in packages)
                {
                    await db.ServerPackages_CreateOrUpdatePackageAsync(
                        Server_Id: context.ServerId,
                        PackageType_Name: "RPM",
                        Package_Name: package.Name,
                        Package_Version: package.Version,
                        CollectedOn_Execution_Id: context.ExecutionId,
                        Url_Text: null,
                        CollectedFor_ServerRole_Id: context.ServerRoleId
                    ).ConfigureAwait(false);
                }
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

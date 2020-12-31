using System;
using Inedo.Extensibility;
using Inedo.Extensibility.Configurations;
using Inedo.Serialization;

namespace Inedo.Extensions.Linux.Configurations
{
    [Serializable]
    [SlimSerializable]
    [ScriptAlias("RPM")]
    public sealed class RpmPackageConfiguration : PackageConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpmPackageConfiguration"/> class.
        /// </summary>
        public RpmPackageConfiguration()
        {
        }
    }
}

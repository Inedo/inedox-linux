using System;
using Inedo.Extensibility;
using Inedo.Extensibility.Configurations;
using Inedo.Serialization;

namespace Inedo.Extensions.Linux.Configurations
{
    /// <summary>
    /// Provides additional metadata for installed Debian packages.
    /// </summary>
    [Serializable]
    [SlimSerializable]
    [ScriptAlias("Debian")]
    public sealed class DebianPackageConfiguration : PackageConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DebianPackageConfiguration"/> class.
        /// </summary>
        public DebianPackageConfiguration()
        {
        }
    }
}

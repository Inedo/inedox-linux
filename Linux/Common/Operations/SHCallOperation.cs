using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
#if Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
#elif BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
#endif

namespace Inedo.Extensions.Linux.Operations
{
    [DisplayName("SHCall")]
    [Description("Calls a shell script that is stored as an asset.")]
    [ScriptAlias("SHCall")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    [DefaultProperty(nameof(ScriptName))]
    public sealed class SHCallOperation : ExecuteOperation
    {
        [Required]
        [ScriptAlias("Name")]
        [DisplayName("Name")]
        [Description("The name of the script asset.")]
        public string ScriptName { get; set; }
        [ScriptAlias("Arguments")]
        [Description("Arguments to pass to the script.")]
        public string Arguments { get; set; }
        [ScriptAlias("Verbose")]
        [Description("When true, additional information about staging the script is written to the debug log.")]
        public bool Verbose { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            using (var scriptReader = SHUtil.OpenScriptAsset(this.ScriptName, this, context))
            {
                if (scriptReader == null)
                    return;

                await SHUtil.ExecuteScriptAsync(context, scriptReader, this.Arguments, this, this.Verbose).ConfigureAwait(false);
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var shortDesc = new RichDescription("SHCall ", new Hilite(config[nameof(this.ScriptName)]));
            var args = config[nameof(this.Arguments)];
            if (string.IsNullOrWhiteSpace(args))
            {
                return new ExtendedRichDescription(shortDesc);
            }
            else
            {
                return new ExtendedRichDescription(
                    shortDesc,
                    new RichDescription(
                        "with arguments ",
                        new Hilite(args)
                    )
                );
            }
        }
    }
}

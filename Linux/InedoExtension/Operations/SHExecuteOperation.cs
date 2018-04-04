using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Web;

namespace Inedo.Extensions.Linux.Operations
{
    [DisplayName("Execute Shell Script")]
    [Description("Executes a specified shell script.")]
    [ScriptAlias("SHExec")]
    [ScriptAlias("Execute-Shell")]
    [ScriptNamespace("Linux", PreferUnqualified = true)]
    [DefaultProperty(nameof(ScriptText))]
    public sealed class SHExecuteOperation : ExecuteOperation
    {
        [Required]
        [ScriptAlias("Text")]
        [Description("The shell script text.")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string ScriptText { get; set; }
        [ScriptAlias("Verbose")]
        [Description("When true, additional information about staging the script is written to the debug log.")]
        public bool Verbose { get; set; }
        [Category("Logging")]
        [ScriptAlias("OutputLogLevel")]
        [DisplayName("Output log level")]
        public MessageLevel OutputLevel { get; set; } = MessageLevel.Information;
        [Category("Logging")]
        [ScriptAlias("ErrorOutputLogLevel")]
        [DisplayName("Error log level")]
        public MessageLevel ErrorLevel { get; set; } = MessageLevel.Error;

        public override Task ExecuteAsync(IOperationExecutionContext context) => SHUtil.ExecuteScriptAsync(context, new StringReader(this.ScriptText), null, this, this.Verbose, this.OutputLevel, this.ErrorLevel);

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Execute ",
                    new Hilite(config[nameof(this.ScriptText)])
                ),
                new RichDescription(
                    "as shell script"
                )
            );
        }
    }
}

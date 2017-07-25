using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Inedo.Agents;
using Inedo.Documentation;
using Inedo.Extensions.Linux.Operations;
#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
#elif Otter
using Inedo.Otter;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensibility.VariableFunctions;
#endif

namespace Inedo.Extensions.Linux.VariableFunctions
{
    [ScriptAlias("SHEval")]
    [Description("Returns the output of a shell script.")]
    [Tag("Linux")]
    [Example(@"
# set the $NextYear variable to the value of... next year
set $ShellScript = >>
date -d next-year +%Y
>>;
set $NextYear = $SHEval($ShellScript);
Log-Information $NextYear;
")]
    public sealed class SHEvalVariableFunction : ScalarVariableFunction
    {
        [DisplayName("script")]
        [VariableFunctionParameter(0)]
        [Description("The shell script to execute. This should be an expression.")]
        public string ScriptText { get; set; }

#if BuildMaster
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
#elif Otter
        protected override object EvaluateScalar(IOtterContext context)
#endif
        {
            var execContext = context as IOperationExecutionContext;
            if (execContext == null)
                throw new NotSupportedException("This function can currently only be used within an execution.");
            if (execContext.Agent.TryGetService<IFileOperationsExecuter>() as ILinuxFileOperationsExecuter == null)
                throw new NotSupportedException("This function is only valid when run against an SSH agent.");

            var output = new StringBuilder();

            SHUtil.ExecuteScriptAsync(execContext, new StringReader(this.ScriptText), null, null, false, data => output.AppendLine(data)).WaitAndUnwrapExceptions();

            return output.ToString();
        }
    }
}

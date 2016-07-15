using System;
using System.IO;
using System.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;

namespace Inedo.Extensions.Linux.Operations
{
    partial class SHUtil
    {
        public static TextReader OpenScriptAsset(string name, ILogger logger, IOperationExecutionContext context)
        {
            string scriptName;
            int? applicationId;
            var scriptNameParts = name.Split(new[] { "::" }, 2, StringSplitOptions.None);
            if (scriptNameParts.Length == 2)
            {
                applicationId = DB.Applications_GetApplications(null, true).FirstOrDefault(a => string.Equals(a.Application_Name, scriptNameParts[0], StringComparison.OrdinalIgnoreCase))?.Application_Id;
                if (applicationId == null)
                {
                    logger.LogError($"Invalid application name {scriptNameParts[0]}.");
                    return null;
                }

                scriptName = scriptNameParts[1];
            }
            else
            {
                applicationId = context.ApplicationId;
                scriptName = scriptNameParts[0];
            }

            if (!scriptName.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
                scriptName += ".sh";

            var script = DB.ScriptAssets_GetScriptByName(scriptName, applicationId);
            if (script == null)
            {
                logger.LogError($"Script {scriptName} not found.");
                return null;
            }

            return new StreamReader(new MemoryStream(script.Script_Text, false), InedoLib.UTF8Encoding);
        }
    }
}

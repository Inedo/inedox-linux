using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Extensions.Linux.Operations;
using Inedo.Web;

namespace Inedo.Extensions.Linux.SuggestionProviders
{
    internal sealed class ScriptNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            if (string.IsNullOrEmpty(config["ScriptName"]))
                return new string[0];

            var currentName = SHUtil.SplitScriptName(config["ScriptName"]);

            var scriptNames = new List<string>();

            using (var raft = RaftRepository.OpenRaft(AH.CoalesceString(currentName.RaftName, RaftRepository.DefaultName), OpenRaftOptions.OptimizeLoadTime | OpenRaftOptions.ReadOnly))
            {
                foreach (var script in await raft.GetRaftItemsAsync(RaftItemType.Script))
                {
                    if (script.ItemName.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
                    {
                        scriptNames.Add(AH.ConcatNE(AH.NullIf(raft.RaftName, RaftRepository.DefaultName), "::") + script.ItemName.Substring(0, script.ItemName.Length - ".sh".Length));
                    }
                }
            }

            return scriptNames;
        }
    }
}

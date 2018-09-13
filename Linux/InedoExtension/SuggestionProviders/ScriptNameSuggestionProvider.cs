using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.ExecutionEngine;
using Inedo.Extensibility;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Web;

namespace Inedo.Extensions.Linux.SuggestionProviders
{
    internal sealed class ScriptNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var currentName = QualifiedName.TryParse(config["ScriptName"]);

            var scriptNames = new List<string>();

            using (var raft = RaftRepository.OpenRaft(AH.CoalesceString(currentName?.Namespace, RaftRepository.DefaultName), OpenRaftOptions.OptimizeLoadTime | OpenRaftOptions.ReadOnly))
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

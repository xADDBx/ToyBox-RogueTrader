using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public class Patch {
    public string PatchId = Guid.NewGuid().ToString();
    public string BlueprintGuid;
    [Obsolete("Added early in development where there was no 1 Patch per Blueprint limit")]
    public List<string> PreviousPatches;
    public List<PatchOperation> Operations;
    public Version PatchVersion = new(1, 0, 0, 0);
    public bool DangerousOperationsEnabled = false;
    public Patch(string blueprintGuid, List<PatchOperation> operations, bool dangerousOperationsEnabled) {
        BlueprintGuid = blueprintGuid;
        Operations = operations;
        DangerousOperationsEnabled = dangerousOperationsEnabled;
    }
}

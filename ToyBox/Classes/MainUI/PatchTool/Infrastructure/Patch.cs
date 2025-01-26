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
    public List<string> PreviousPatches;
    public List<PatchOperation> Operations;
    public Version PatchVersion = new(1, 0, 0, 0);
    public Patch(string blueprintGuid, List<PatchOperation> operations, List<string> previousPatches = null) {
        BlueprintGuid = blueprintGuid;
        Operations = operations;
        PreviousPatches = previousPatches;
    }
}

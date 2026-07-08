namespace ToyBox.Features.PatchTool.Infrastructure;
public class Patch {
    public string PatchId = Guid.NewGuid().ToString();
    public string BlueprintGuid;
    public List<PatchOperation> Operations;
    public Version PatchVersion = new(1, 0, 0, 0);
    public bool DangerousOperationsEnabled = false;
    public Patch(string blueprintGuid, List<PatchOperation> operations, bool dangerousOperationsEnabled) {
        BlueprintGuid = blueprintGuid;
        Operations = operations;
        DangerousOperationsEnabled = dangerousOperationsEnabled;
    }
}

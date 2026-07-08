using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Exchanges a UnityEngine.Object-typed field for a different asset that is already registered in
// the asset resolution system (UnityObjectConverter.AssetList / ModificationAssetLists),
// identified by the (AssetId, FileId) pair
public sealed class ModifyUnityReferenceOperation : PatchOperation {
    public string AssetId;
    public long FileId;
    public ModifyUnityReferenceOperation(string fieldName, Type patchedObjectType, string assetId, long fileId) : base(fieldName, patchedObjectType) {
        AssetId = assetId;
        FileId = fileId;
    }
    public override string Kind {
        get {
            return "ModifyUnityReference";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var asset = PatchToolUtils.ResolveUnityAsset(AssetId, FileId)
            ?? throw new ArgumentException($"Could not resolve a Unity asset for guid '{AssetId}' fileid {FileId}.");
        var field = ResolveField(true, asset.GetType());
        if (field != null) {
            inverse = new RestoreValueOperation(FieldName, PatchedObjectType, field.GetValue(instance));
            field.SetValue(instance, asset);
            return instance;
        }
        inverse = new RestoreValueOperation(FieldName, PatchedObjectType, instance, isElementMode: true);
        return asset;
    }
}

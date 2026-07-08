using System.Runtime.CompilerServices;

namespace ToyBox.Features.PatchTool.Utils;

public class ReferenceEqualityComparer : EqualityComparer<object> {
    public override bool Equals(object x, object y) {
        return ReferenceEquals(x, y);
    }
    public override int GetHashCode(object obj) {
        if (obj == null) {
            return 0;
        }
        // E.g. WeakResourceLink can throw on GetHashCode()
        // return obj.GetHashCode();
        return RuntimeHelpers.GetHashCode(obj);
    }
}

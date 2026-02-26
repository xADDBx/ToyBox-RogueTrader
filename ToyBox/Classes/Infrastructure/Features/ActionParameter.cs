using Kingmaker.EntitySystem.Entities;
using System.Text;

namespace ToyBox.Classes.Infrastructure.Features;

public readonly struct ActionParameter : IEquatable<ActionParameter> {
    public readonly int IntParam {
        get {
            return m_IntParam ?? 1;
        }
    }
    private readonly int? m_IntParam;
    public readonly BaseUnitEntity? UnitParam { get; }
    public static ActionParameter Empty {
        get {
            return default;
        }
    }
    public ActionParameter(int param) {
        m_IntParam = param;
    }
    public ActionParameter(BaseUnitEntity? unitParam, int? intParam = null) {
        UnitParam = unitParam;
        m_IntParam = intParam;
    }
    public override readonly string ToString() {
        if (this == Empty) {
            return "";
        }
        var result = new StringBuilder();
        _ = result.Append("ActionParameter [");
        var hasFirst = false;
        if (UnitParam != null) {
            _ = result.Append($"{UnitParam}");
            hasFirst = true;
        }
        if (m_IntParam != null) {
            if (hasFirst) {
                _ = result.Append(", ");
            }
            _ = result.Append($"{m_IntParam}");
        }
        _ = result.Append(']');
        return result.ToString();
    }

    public override readonly int GetHashCode() {
        return ((m_IntParam ?? -1) * 31) + (UnitParam?.GetHashCode() ?? 0);
    }

    public static bool operator ==(ActionParameter left, ActionParameter right) {
        return left.Equals(right);
    }

    public static bool operator !=(ActionParameter left, ActionParameter right) {
        return !(left == right);
    }

    public override readonly bool Equals(object obj) {
        return obj is ActionParameter param && Equals(param);
    }

    public readonly bool Equals(ActionParameter other) {
        return m_IntParam == other.m_IntParam && UnitParam == other.UnitParam;
    }
}

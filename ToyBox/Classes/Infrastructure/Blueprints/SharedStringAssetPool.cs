using Kingmaker.Localization;
using Owlcat.Runtime.Core.Utility;
using System.Collections.Concurrent;

namespace ToyBox.Infrastructure.Blueprints;
public class SharedStringAssetPool : MonoSingleton<SharedStringAssetPool> {
    public const int Size = 100;
    private readonly ConcurrentQueue<SharedStringAsset> m_Pool = new();
    private SemaphoreSlim m_Available = null!;
    private void Awake() {
        m_Available = new SemaphoreSlim(0, int.MaxValue);
        for (var i = 0; i < Size; i++) {
            var inst = UnityEngine.ScriptableObject.CreateInstance<SharedStringAsset>();
            m_Pool.Enqueue(inst);
            _ = m_Available.Release();
        }
    }
    private void OnDestroy() {
        while (m_Pool.TryDequeue(out var result)) {
            Destroy(result);
        }
        m_Available.Dispose();
    }
    private void Update() {
        var toAdd = Size - m_Pool.Count;
        for (var i = 0; i < toAdd; i++) {
            var inst = UnityEngine.ScriptableObject.CreateInstance<SharedStringAsset>();
            m_Pool.Enqueue(inst);
            _ = m_Available.Release();
        }
    }
    public SharedStringAsset Request() {
        if (UnityEngine.Object.CurrentThreadIsMainThread()) {
            return UnityEngine.ScriptableObject.CreateInstance<SharedStringAsset>();
        } else {
            m_Available.Wait();
            if (m_Pool.TryDequeue(out var requested)) {
                return requested;
            } else {
                Critical($"SharedStringAssetPool had no instance in pool despite signalling availability!", true);
                throw new InvalidOperationException($"SharedStringAssetPool had no instance in pool");
            }
        }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;

namespace ToyBox;

public abstract partial class FeatureTab {
    private static readonly ConcurrentDictionary<Type, FeatureTab> m_Instances = [];
    public static readonly HashSet<Feature> FailedFeatures = [];
    private Dictionary<string, List<Feature>> m_FeatureGroups { get; set; } = [];
    public abstract string Name { get; }
    public virtual bool IsHiddenFromUI {
        get {
            return false;
        }
    }
    protected FeatureTab() {
        var t = GetType();
        if (!m_Instances.TryAdd(t, this)) {
            throw new InvalidOperationException($"FeatureTab of type {t.Name} was already constructed.");
        }
    }
    public static T GetInstance<T>() where T : FeatureTab {
        if (!m_Instances.TryGetValue(typeof(T), out var inst)) {
            throw new InvalidOperationException($"No constructed instance of type {typeof(T)} found!");
        }
        return (T)inst;
    }
    public virtual void AddFeature(Feature feature, string groupName = "") {
        if (feature is INeedEarlyInitFeature) {
            try {
                feature.Initialize();
            } catch (Exception ex) {
                Error($"Failed to early initialize feature {feature.Name}\n{ex}", false);
                feature.Unload();
                _ = FailedFeatures.Add(feature);
            }
        }
        if (!m_FeatureGroups.TryGetValue(groupName, out var group)) {
            group = [];
            m_FeatureGroups[groupName] = group;
        }
        group.Add(feature);
    }
    public IEnumerable<Feature> Features {
        get {
            foreach (var group in m_FeatureGroups.Values) {
                foreach (var feature in group) {
                    yield return feature;
                }
            }
        }
    }

    public IEnumerable<(string groupName, List<Feature> features)> Groups {
        get {
            foreach (var group in m_FeatureGroups) {
                yield return (group.Key, group.Value);
            }
        }
    }

    public int GroupCount {
        get {
            return m_FeatureGroups.Count;
        }
    }
    public virtual void InitializeAll() {
        var a = Stopwatch.StartNew();
        foreach (var feature in Features) {
            if (feature is not INeedEarlyInitFeature) {
                try {
                    feature.Initialize();
                } catch (Exception ex) {
                    Error($"Failed to initialize feature {feature.Name}\n{ex}", false);
                    feature.Unload();
                    _ = FailedFeatures.Add(feature);
                }
            }
        }
        Debug($"!!Threaded!!: {GetType().Name} lazy init took {a.ElapsedMilliseconds}ms");
    }
    public virtual void DestroyAll() {
        foreach (var feature in Features) {
            feature.Unload();
        }
    }
    public virtual void OnGui() {
        var i = 0;
        foreach (var (groupName, features) in Groups) {
            i++;
            using (VerticalScope()) {
                if (!string.IsNullOrWhiteSpace(groupName)) {
                    UI.Label(groupName);
                }
                using (HorizontalScope()) {
                    Space(25);
                    using (VerticalScope()) {
                        foreach (var feature in features) {
                            if (!feature.ShouldHide) {
                                if (FailedFeatures.Contains(feature)) {
                                    UI.Label((m_FeatureFailedInitializationLocalizedText + ": ").Orange().Bold() + feature.Name.Cyan());
                                } else {
                                    feature.OnGui();
                                }
                            }
                        }
                    }
                }
            }
            if (i < GroupCount) {
                Div.DrawDiv();
            }
        }
    }
    [LocalizedString("ToyBox_FeatureTab_m_FeatureFailedInitializationLocalizedText", "Feature failed initialization")]
    private static partial string m_FeatureFailedInitializationLocalizedText { get; }
}

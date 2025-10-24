using System.Diagnostics;

namespace ToyBox;
public abstract class FeatureTab {
    public List<Feature> FailedFeatures = [];
    private Dictionary<string, List<Feature>> m_FeatureGroups { get; set; } = [];
    public abstract string Name { get; }
    public virtual bool IsHiddenFromUI {
        get {
            return false;
        }
    }

    public virtual void AddFeature(Feature feature, string groupName = "") {
        if (feature is INeedEarlyInitFeature) {
            try {
                feature.Initialize();
            } catch (Exception ex) {
                Error($"Failed to early initialize feature {feature.Name}\n{ex}", 1, false);
                FailedFeatures.Add(feature);
            }
        }
        if (!m_FeatureGroups.TryGetValue(groupName, out var group)) {
            group = [];
            m_FeatureGroups[groupName] = group;
        }
        group.Add(feature);
    }
    public IEnumerable<Feature> GetFeatures() {
        foreach (var group in m_FeatureGroups.Values) {
            foreach (var feature in group) {
                yield return feature;
            }
        }
    }
    public IEnumerable<(string groupName, List<Feature> features)> GetGroups() {
        foreach (var group in m_FeatureGroups) {
            yield return (group.Key, group.Value);
        }
    }
    public virtual void InitializeAll() {
        var a = Stopwatch.StartNew();
        foreach (var feature in GetFeatures()) {
            if (feature is not INeedEarlyInitFeature) {
                try {
                    feature.Initialize();
                } catch (Exception ex) {
                    Error($"Failed to initialize feature {feature.Name}\n{ex}", 1, false);
                    feature.Destroy();
                    FailedFeatures.Add(feature);
                }
            }
        }
        Debug($"!!Threaded!!: {GetType().Name} lazy init took {a.ElapsedMilliseconds}ms");
    }
    public virtual void DestroyAll() {
        foreach (var feature in GetFeatures()) {
            feature.Destroy();
        }
    }
    public virtual void OnGui() {
        foreach (var (groupName, features) in GetGroups()) {
            using (VerticalScope()) {
                UI.Label(groupName);
                using (HorizontalScope()) {
                    Space(25);
                    using (VerticalScope()) {
                        foreach (var feature in features) {
                            if (!feature.ShouldHide) {
                                feature.OnGui();
                            }
                        }
                    }
                }
            }
            Div.DrawDiv();
        }
    }
}

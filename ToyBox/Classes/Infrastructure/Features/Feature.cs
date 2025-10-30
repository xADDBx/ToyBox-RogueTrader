﻿using System.Collections.Concurrent;

namespace ToyBox;
public abstract class Feature {
    private static readonly ConcurrentDictionary<Type, Feature> m_Instances = [];
    protected Feature() {
        var t = GetType();
        if (!m_Instances.TryAdd(t, this)) {
            throw new InvalidOperationException($"Feature of type {t.Name} was already constructed.");
        }
    }
    public static T GetInstance<T>() where T : Feature {
        if (!m_Instances.TryGetValue(typeof(T), out var inst)) {
            // inst = (T)Activator.CreateInstance(typeof(T), true);
            // m_Instances[typeof(T)] = inst;
            throw new InvalidOperationException($"No constructed instance of type {typeof(T)} found!");
        }
        return (T)inst;
    }
    public static T GetInstance<T>(Type t) where T : Feature{
        if (!m_Instances.TryGetValue(t, out var inst)) {
            throw new InvalidOperationException($"No constructed instance of type {typeof(T)} found!");
        }
        return (T)inst;
    }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract void OnGui();
    public virtual void Initialize() { }
    public virtual void Destroy() { }
    public virtual bool ShouldHide {
        get {
            return false;
        }
    }

    public virtual string SortKey {
        get {
            return Name;
        }

        set { }
    }
    public virtual string SearchKey {
        get {
            return $"{Name} {Description}";
        }

        set { }
    }
}

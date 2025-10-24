using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.Localization;
using Kingmaker.Modding;
using Kingmaker.Utility.DotNetExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints;
#warning Add Sequential Mode
public class BlueprintLoader {
    private List<SimpleBlueprint?> m_BlueprintBeingLoaded = null!;
    private List<SimpleBlueprint>? m_Blueprints;
    private readonly Dictionary<Type, List<SimpleBlueprint>> m_BlueprintsByType = [];
    private readonly HashSet<SimpleBlueprint> m_BlueprintsToAdd = [];
    private readonly HashSet<string> m_BlueprintsToRemove = [];
    public bool CanStart = false;
    public static BlueprintLoader BPLoader { get; } = new();
    // public readonly HashSet<string> BadBlueprints = ["ce0842546b73aa34b8fcf40a970ede68", "2e3280bf21ec832418f51bee5136ec7a", "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082", "5d2b9742ce82457a9ae7209dce770071"];
    public BlueprintLoader() {
        _ = SharedStringAssetPool.Instance;

        var toPatch = AccessTools.Method(typeof(StartGameLoader), nameof(StartGameLoader.LoadPackTOC));
        var patch = AccessTools.Method(typeof(BlueprintLoader), nameof(InitPatch));
        _ = Main.HarmonyInstance.Patch(toPatch, finalizer: new(patch));

        toPatch = AccessTools.Method(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint));
        patch = AccessTools.Method(typeof(BlueprintLoader), nameof(AddCachedBlueprintPatch));
        _ = Main.HarmonyInstance.Patch(toPatch, postfix: new(patch));

        toPatch = AccessTools.Method(typeof(BlueprintsCache), nameof(BlueprintsCache.RemoveCachedBlueprint));
        patch = AccessTools.Method(typeof(BlueprintLoader), nameof(RemoveCachedBlueprintPatch));
        _ = Main.HarmonyInstance.Patch(toPatch, prefix: new(patch));

        toPatch = AccessTools.Method(typeof(BlueprintsCache), nameof(BlueprintsCache.Load));
        patch = AccessTools.Method(typeof(BlueprintLoader), nameof(BlueprintsCache_LoadPrefix));
        var patch2 = AccessTools.Method(typeof(BlueprintLoader), nameof(BlueprintsCache_LoadPostfix));
        _ = Main.HarmonyInstance.Patch(toPatch, prefix: new(patch), postfix: new(patch2));

        toPatch = AccessTools.Method(typeof(OwlcatModificationBlueprintPatcher), nameof(OwlcatModificationBlueprintPatcher.ApplyPatchEntry));
        patch = AccessTools.Method(typeof(BlueprintLoader), nameof(OwlcatModificationBlueprintPatcher_ApplyPatchEntry));
        _ = Main.HarmonyInstance.Patch(toPatch, prefix: new(patch));

        toPatch = AccessTools.Method(typeof(OwlcatModificationBlueprintPatcher), nameof(OwlcatModificationBlueprintPatcher.GetJObject));
        patch = AccessTools.Method(typeof(BlueprintLoader), nameof(OwlcatModificationBlueprintPatcher_GetJObject));
        _ = Main.HarmonyInstance.Patch(toPatch, prefix: new(patch));

        toPatch = AccessTools.Method(typeof(SharedStringConverter), nameof(SharedStringConverter.ReadJson));
        patch = AccessTools.Method(typeof(BlueprintLoader), nameof(SharedStringConverter_ReadJson_Patch));
        _ = Main.HarmonyInstance.Patch(toPatch, prefix: new(patch));
    }
    public List<SimpleBlueprint>? GetBlueprints(Action<IEnumerable<SimpleBlueprint>>? blueprintsAreLoadedCallback = null) {
        if (m_Blueprints == null) {
            lock (this) {
                if (blueprintsAreLoadedCallback != null) {
                    _ = m_OnFinishLoadingCallback.Add(blueprintsAreLoadedCallback);
                }
                if (IsLoading) {
                    return null;
                } else {
                    Debug($"Starting Blueprint Loading");
                    Load((bps) => {
                        lock (m_BlueprintsToAdd) {
                            bps.AddRange(m_BlueprintsToAdd);
                            m_BlueprintsToAdd.Clear();
                        }
                        lock (m_BlueprintsToRemove) {
                            _ = bps.RemoveAll(bp => m_BlueprintsToRemove.Contains(bp.AssetGuid));
                            m_BlueprintsToRemove.Clear();
                        }
                        m_Blueprints = bps;
                        if (BlueprintIdCache.NeedsCacheRebuilt) {
                            BlueprintIdCache.RebuildCache(m_Blueprints);
                        }
                        m_BlueprintsByType.Clear();
                    });
                    return null;
                }
            }
        } else {
            if (m_BlueprintsToAdd.Count > 0) {
                lock (m_BlueprintsToAdd) {
                    m_Blueprints.AddRange(m_BlueprintsToAdd);
                    m_BlueprintsToAdd.Clear();
                }
            }
            if (m_BlueprintsToRemove.Count > 0) {
                lock (m_BlueprintsToRemove) {
                    _ = m_Blueprints.RemoveAll(bp => m_BlueprintsToRemove.Contains(bp.AssetGuid));
                    m_BlueprintsToRemove.Clear();
                }
            }
            blueprintsAreLoadedCallback?.Invoke(m_Blueprints);
            return m_Blueprints;
        }
    }
    public static IEnumerable<SimpleBlueprint>? BlueprintsOfType(Type type, Action<IEnumerable<SimpleBlueprint>>? onFinishLoadingCallback = null) {
        return (IEnumerable<SimpleBlueprint>)AccessTools.Method(typeof(BlueprintLoader), nameof(GetBlueprintsOfType)).MakeGenericMethod(type).Invoke(BPLoader, [onFinishLoadingCallback]);
    }

    public IEnumerable<BPType>? GetBlueprintsOfType<BPType>(Action<IEnumerable<BPType>>? onFinishLoadingCallback = null) where BPType : SimpleBlueprint {
        if (m_Blueprints == null) {
            if (Settings.UseBPIdCache && !BlueprintIdCache.NeedsCacheRebuilt) {
                if (m_BlueprintsByType.TryGetValue(typeof(BPType), out var bps)) {
                    var bps2 = bps.Cast<BPType>();
                    onFinishLoadingCallback?.Invoke(bps2);
                    return bps2;
                } else if (BlueprintIdCache.Instance.IdsByType.TryGetValue(typeof(BPType), out var ids)) {
                    Load(bps => {
                        lock (m_BlueprintsToAdd) {
                            // OfType is lazy; fully evaluate the collection to prevent InvalidOperationException
                            IEnumerable<BPType> toAdd = [.. m_BlueprintsToAdd.OfType<BPType>()];
                            if (toAdd != null) {
                                m_BlueprintsToAdd.ExceptWith(toAdd);
                                bps.AddRange(toAdd);
                            }
                        }
                        // Is this really necessary?
                        lock (m_BlueprintsToRemove) {
                            if (m_BlueprintsToRemove.Count > 0) {
                                bps.ToArray().ForEach(bp => {
                                    if (m_BlueprintsToRemove.Contains(bp.AssetGuid)) {
                                        _ = m_BlueprintsToRemove.Remove(bp.AssetGuid);
                                        _ = bps.Remove(bp);
                                    }
                                });
                            }
                        }
                        m_BlueprintsByType[typeof(BPType)] = bps;
                        onFinishLoadingCallback?.Invoke(bps.Cast<BPType>());
                    }, ids);
                    return null;
                }
            }
            return GetBlueprints((onFinishLoadingCallback == null) ? null : (IEnumerable<SimpleBlueprint> bps2) => onFinishLoadingCallback(bps2.OfType<BPType>()))
                ?.OfType<BPType>();
        } else {
            IEnumerable<BPType>? bps = null;
            if (Settings.UseBPIdCache && !BlueprintIdCache.NeedsCacheRebuilt) {
                if (m_BlueprintsByType.TryGetValue(typeof(BPType), out var bps2)) {
                    bps = bps2.Cast<BPType>();
                }
            }
            bps ??= m_Blueprints.OfType<BPType>();

            return bps;
        }
    }
    public IEnumerable<BPType> GetBlueprintsByGuids<BPType>(IEnumerable<string> guids) where BPType : SimpleBlueprint {
        foreach (var guid in guids) {
            if (ResourcesLibrary.TryGetBlueprint(guid) is BPType bp) {
                yield return bp;
            }
        }
    }
    private int m_TotalLoading;
    private int m_EstimateLoaded;
    private readonly HashSet<Action<IEnumerable<SimpleBlueprint>>> m_OnFinishLoadingCallback = [];
    private Action<List<SimpleBlueprint>> m_OnFinishLoading = null!;
    private readonly List<ConcurrentDictionary<string, object>> m_StartedLoadingShards = [];
    private readonly List<Task> m_WorkerTasks = [];
    private ConcurrentQueue<IEnumerable<(string bpToLoad, int index)>> m_ChunkQueue = null!;
    private void Load(Action<List<SimpleBlueprint>> callback, ISet<string>? toLoad = null) {
        // If:
        // 1. Is Loading
        // 2. Or: Is not set as startable and has null m_PackFile (if Hotreloading is used, CanStart is false even though it should be possible to load
        // 3. Or: Already loaded
        if (IsLoading || (!CanStart && ResourcesLibrary.BlueprintsCache.m_PackFile == null) || m_Blueprints != null) {
            return;
        }

        m_EstimateLoaded = 0;
        m_StartedLoadingShards.Clear();
        for (var i = 0; i < Settings.BlueprintsLoaderNumShards; i++) {
            m_StartedLoadingShards.Add(new());
        }
        m_OnFinishLoading = callback;
        m_WorkerTasks.Clear();
        IsLoading = true;
        _ = Task.Run(() => Run(toLoad));
    }
    public float Progress {
        get {
            if (ImguiCanChangeStateAtBeginning()) {
                if (m_TotalLoading > 0) {
                    field = (float)m_EstimateLoaded / m_TotalLoading;
                } else {
                    field = 0;
                }
            }
            return field;
        }
    }
    public bool IsLoading = false;
    public bool HasLoaded {
        get {
            return m_Blueprints != null;
        }
    }

    public void Run(ISet<string>? toLoad) {
        try {
            var watch = Stopwatch.StartNew();
            var bpCache = ResourcesLibrary.BlueprintsCache;
            IEnumerable<string> allEntries;
            var toc = bpCache.m_LoadedBlueprints;
            if (toLoad == null) {
                allEntries = toc.OrderBy(e => e.Value.Offset).Select(e => e.Key);
            } else {
                allEntries = toc.Where(item => toLoad.Contains(item.Key)).OrderBy(e => e.Value.Offset).Select(e => e.Key);
            }
            m_TotalLoading = allEntries.Count();
            Log($"Loading {m_TotalLoading} Blueprints");
            m_BlueprintBeingLoaded = new(m_TotalLoading);
            m_BlueprintBeingLoaded.AddRange(Enumerable.Repeat<SimpleBlueprint?>(null, m_TotalLoading));
            var memStream = new MemoryStream();
            lock (bpCache.m_Lock) {
                bpCache.m_PackFile.Position = 0;
                bpCache.m_PackFile.CopyTo(memStream);
            }
            var chunks = allEntries.Select((entry, index) => (entry, index)).Chunk(Settings.BlueprintsLoaderChunkSize);
            m_ChunkQueue = new(chunks);
            var bytes = memStream.GetBuffer();
            for (var i = 0; i < Settings.BlueprintsLoaderNumThreads; i++) {
                var t = Task.Run(() => HandleChunks(bytes));
                m_WorkerTasks.Add(t);
            }
            foreach (var task in m_WorkerTasks) {
                task.Wait();
            }
            _ = m_BlueprintBeingLoaded.RemoveAll(b => b is null);
            watch.Stop();
            Log($"Threaded loaded roughly {m_BlueprintBeingLoaded.Count + m_BlueprintsToAdd.Count + m_LoadingSequentially.Value.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            toLoad = null;
            lock (this) {
                m_OnFinishLoading(m_BlueprintBeingLoaded!);
                foreach (var callback in m_OnFinishLoadingCallback) {
                    callback(m_BlueprintBeingLoaded!);
                }
                m_OnFinishLoadingCallback.Clear();
                new Action(() => {
                    IsLoading = false;
                }).ScheduleForMainThread();
            }
        } catch (Exception ex) {
            Critical(ex);
            throw;
        }
    }
    // External mods could register their own actions here
    public Action<SimpleBlueprint>? OnAfterBPLoad = null;
    public Action<string>? OnBeforeBPLoad = null;
    public void HandleChunks(byte[] bytes) {
        try {
            Stream stream = new MemoryStream(bytes) {
                Position = 0
            };
            var seralizer = new ReflectionBasedSerializer(new PrimitiveSerializer(new BinaryReader(stream), UnityObjectConverter.AssetList));
            var closeCountLocal = 0;
            while (m_ChunkQueue.TryDequeue(out var blueprintChunk)) {
                if (closeCountLocal > 100) {
                    lock (m_BlueprintBeingLoaded) {
                        m_EstimateLoaded += closeCountLocal;
                    }
                    closeCountLocal = 0;
                }
                foreach (var (bpToLoad, index) in blueprintChunk) {
                    var guid = bpToLoad;
                    try {
                        object @lock = new();
                        lock (@lock) {
                            var shardIndex = Math.Abs(guid.GetHashCode()) % Settings.BlueprintsLoaderNumShards;
                            var startedLoading = m_StartedLoadingShards[shardIndex];
                            if (!startedLoading.TryAdd(guid, @lock)) {
                                continue;
                            }
                            if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.TryGetValue(guid, out var entry)) {
                                if (entry.Blueprint != null) {
                                    closeCountLocal++;
                                    m_BlueprintBeingLoaded[index] = entry.Blueprint;
                                    continue;
                                }
                            } else {
                                continue;
                            }
                            if (/* BadBlueprints.Contains(guid) || */entry.Offset == 0U) {
                                continue;
                            }
                            OnBeforeBPLoad?.Invoke(guid);
                            _ = stream.Seek(entry.Offset, SeekOrigin.Begin);
                            SimpleBlueprint? simpleBlueprint = null;
                            seralizer.Blueprint(ref simpleBlueprint);
                            if (simpleBlueprint == null) {
                                closeCountLocal++;
                                continue;
                            }
                            var obj = ResourcesLibrary.BlueprintsCache.m_resourceReplacementProvider?.OnResourceLoaded(simpleBlueprint, guid);
                            simpleBlueprint = (obj as SimpleBlueprint) ?? simpleBlueprint;
                            entry.Blueprint = simpleBlueprint;
                            simpleBlueprint.OnEnable();
                            m_BlueprintBeingLoaded[index] = simpleBlueprint;
                            ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[guid] = entry;
                            closeCountLocal++;
                            OnAfterBPLoad?.Invoke(simpleBlueprint);
                        }
                    } catch (Exception ex) {
                        Warn($"Exception loading blueprint {guid}:\n{ex}");
                        closeCountLocal++;
                    }
                }
            }
        } catch (Exception ex) {
            Error($"Exception loading blueprints:\n{ex}");
        }
    }
    private static void AddCachedBlueprintPatch(string guid, SimpleBlueprint bp) {
        if (BPLoader.IsLoading || BPLoader.m_Blueprints != null) {
            if (BPLoader.IsLoading) {
                var shardIndex = Math.Abs(guid.GetHashCode()) % Settings.BlueprintsLoaderNumShards;
                _ = BPLoader.m_StartedLoadingShards[shardIndex].TryAdd(guid, BPLoader);
            }
            lock (BPLoader.m_BlueprintsToAdd) {
                _ = BPLoader.m_BlueprintsToAdd.Add(bp);
            }
        }
    }
    private static void RemoveCachedBlueprintPatch(string guid) {
        lock (BPLoader.m_BlueprintsToRemove) {
            _ = BPLoader.m_BlueprintsToRemove.Add(guid);
        }
    }
    private static readonly ThreadLocal<HashSet<string>> m_LoadingSequentially = new(() => []);
    private static bool BlueprintsCache_LoadPrefix(string guid, ref SimpleBlueprint __result) {
        // If threaded loader is not activated just load normally
        if (!BPLoader.IsLoading) {
            return true;
        }
        var shardIndex = Math.Abs(guid.GetHashCode()) % Settings.BlueprintsLoaderNumShards;
        var startedLoading = BPLoader.m_StartedLoadingShards[shardIndex];
        if (startedLoading.TryAdd(guid, BPLoader)) {
            // If the requested bp was not yet touched by the threaded loader, just load normally
            _ = m_LoadingSequentially.Value.Add(guid);
            return true;
        }
        // The requested bp was touched by the threaded loader, so lock on the object to wait for the loading to complete
        lock (startedLoading[guid]) {
            if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.TryGetValue(guid, out var entry)) {
                __result = entry.Blueprint;
            } else {
                __result = null!;
            }
        }
        return false;
    }
    private static void BlueprintsCache_LoadPostfix(string guid, ref SimpleBlueprint __result) {
        if (m_LoadingSequentially.Value.Remove(guid)) {
            lock (BPLoader.m_BlueprintsToAdd) {
                if (__result != null) {
                    _ = BPLoader.m_BlueprintsToAdd.Add(__result);
                }
            }
        }
    }
    private static void InitPatch() {
        BPLoader.CanStart = true;
        if (Settings.PreloadBlueprints || (Settings.UseBPIdCache && Settings.AutomaticallyBuildBPIdCache && BlueprintIdCache.NeedsCacheRebuilt)) {
            _ = BPLoader.GetBlueprints();
        }
    }

    private static bool OwlcatModificationBlueprintPatcher_ApplyPatchEntry(JObject jsonBlueprint, JObject patchEntry) {
        JsonMergeSettings settings = new() {
            MergeArrayHandling = OwlcatModificationBlueprintPatcher.ExtractMergeArraySettings(patchEntry),
            MergeNullValueHandling = OwlcatModificationBlueprintPatcher.ExtractNullArraySettings(patchEntry)
        };
        jsonBlueprint.Merge(patchEntry, settings);
        return false;
    }
    private static readonly ConcurrentDictionary<SimpleBlueprint, JObject> m_JsonBlueprintsCache = [];
    [ThreadStatic]
    private static StringBuilder? m_Builder;
    private static bool OwlcatModificationBlueprintPatcher_GetJObject(SimpleBlueprint blueprint, ref JObject __result) {
        if (m_JsonBlueprintsCache.TryGetValue(blueprint, out var jsonBlueprint)) {
            __result = jsonBlueprint;
            return false;
        }
        var blueprintJsonWrapper = new BlueprintJsonWrapper(blueprint) {
            AssetId = blueprint.AssetGuid
        };
        m_Builder ??= new(64);
        using var stringWriter = new StringWriter(m_Builder);
        Json.Serializer.Serialize(stringWriter, blueprintJsonWrapper);
        var jobject = JObject.Parse(m_Builder.ToString());
        m_JsonBlueprintsCache[blueprint] = jobject;
        __result = jobject;
        _ = m_Builder.Clear();
        return false;
    }
    private static bool SharedStringConverter_ReadJson_Patch(ref object? __result, JsonReader reader) {
        if (reader.TokenType == JsonToken.Null) {
            __result = null;
            return false;
        }
        var text = (string)JObject.Load(reader)["stringkey"]!;
        var sharedStringAsset = SharedStringAssetPool.Instance.Request();
        sharedStringAsset.String = new LocalizedString {
            Key = text
        };
        __result = sharedStringAsset;
        return false;
    }
}

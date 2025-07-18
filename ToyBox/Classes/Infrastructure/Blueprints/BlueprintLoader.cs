// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.Modding;
using ModKit;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ToyBox.classes.Infrastructure.Blueprints;

namespace ToyBox {
    public class BlueprintLoader {
        public static string GameVersion;
        public delegate void LoadBlueprintsCallback(List<SimpleBlueprint> blueprints);
        private List<SimpleBlueprint> blueprints;
        private Dictionary<Type, List<SimpleBlueprint>> bpsByType = new();
        private HashSet<SimpleBlueprint> bpsToAdd = new();
        internal bool CanStart = false;
        public float progress = 0;
        private static BlueprintLoader loader;
        public static BlueprintLoader Shared {
            get {
                if (GameVersion.IsNullOrEmpty()) {
                    GameVersion = Kingmaker.GameInfo.GameVersion.GetVersion();
                }
                loader ??= new();
                return loader;
            }
        }
        internal readonly HashSet<string> BadBlueprints = new() { "ce0842546b73aa34b8fcf40a970ede68", "2e3280bf21ec832418f51bee5136ec7a",
            "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082", "5d2b9742ce82457a9ae7209dce770071" };
        private void Load(LoadBlueprintsCallback callback, ISet<string> toLoad = null) {
            lock (loader) {
                if (IsLoading || (!CanStart && Game.Instance.Player == null) || blueprints != null) return;
                loader.Init(callback, toLoad);
            }
        }
        public bool IsLoading => loader.IsRunning;
        public List<SimpleBlueprint> GetBlueprints() {
            if (blueprints == null) {
                lock (loader) {
                    if (Shared.IsLoading) {
                        return null;
                    } else {
                        Mod.Debug($"Calling BlueprintLoader.Load");
                        Shared.Load((bps) => {
                            lock (bpsToAdd) {
                                bps.AddRange(bpsToAdd);
                                bpsToAdd.Clear();
                            }
                            blueprints = bps;
                            if (BlueprintIdCache.NeedsCacheRebuilt) BlueprintIdCache.RebuildCache(blueprints);
                            bpsByType.Clear();
                        }, toLoad);
                        return null;
                    }
                }
            }
            lock (bpsToAdd) {
                if (bpsToAdd.Count > 0) {
                    blueprints.AddRange(bpsToAdd);
                    bpsToAdd.Clear();
                }
            }
            return blueprints;
        }
        public static IEnumerable<SimpleBlueprint> BlueprintsOfType(Type type) {
            return (IEnumerable<SimpleBlueprint>)AccessTools.Method(typeof(BlueprintLoader), nameof(GetBlueprintsOfType)).MakeGenericMethod(type).Invoke(Shared, null);
        }
        public IEnumerable<BPType> GetBlueprintsOfType<BPType>() where BPType : SimpleBlueprint {
            if (blueprints == null) {
                if (Main.Settings.toggleUseBPIdCache && !BlueprintIdCache.NeedsCacheRebuilt) {
                    if (bpsByType.TryGetValue(typeof(BPType), out var bps)) {
                        return bps.Cast<BPType>();
                    } else if (BlueprintIdCache.Instance.IdsByType.TryGetValue(typeof(BPType), out var ids)) {
                        if (!Shared.IsLoading) {
                            Load(bps => {
                                IEnumerable<BPType> toAdd;
                                toAdd = bpsToAdd.OfType<BPType>() ?? new List<BPType>();
                                bps.AddRange(toAdd);
                                bpsByType[typeof(BPType)] = bps;
                            }, ids);
                        }
                        return new List<BPType>();
                    }
                }
            } else {
                return blueprints.OfType<BPType>();
            }
            return GetBlueprints()?.OfType<BPType>() ?? new List<BPType>();
        }
        public IEnumerable<BPType> GetBlueprintsByGuids<BPType>(IEnumerable<string> guids) where BPType : SimpleBlueprint {
            foreach (var guid in guids) {
                var bp = ResourcesLibrary.TryGetBlueprint(guid) as BPType;
                if (bp != null) {
                    yield return bp;
                }
            }
        }
        public bool IsRunning = false;
        private LoadBlueprintsCallback _callback;
        private List<Task> _workerTasks;
        private ConcurrentQueue<IEnumerable<(string, int)>> _chunkQueue;
        private List<SimpleBlueprint> _blueprints;
        private List<ConcurrentDictionary<string, Object>> _startedLoadingShards = new();
        private int closeCount;
        private int total;
        private ISet<string> toLoad;
        public void Init(LoadBlueprintsCallback callback, ISet<string> toLoad) {
            closeCount = 0;
            _startedLoadingShards.Clear();
            for (int i = 0; i < Main.Settings.BlueprintsLoaderNumShards; i++) {
                _startedLoadingShards.Add(new());
            }
            _callback = callback;
            _workerTasks = new();
            this.toLoad = toLoad;
            IsRunning = true;
            Task.Run(Run);
        }
        public void Run() {
            var watch = Stopwatch.StartNew();
            var bpCache = ResourcesLibrary.BlueprintsCache;
            IEnumerable<string> allEntries;
            var toc = bpCache.m_LoadedBlueprints;
            if (toLoad == null) {
                allEntries = toc.OrderBy(e => e.Value.Offset).Select(e => e.Key);
            } else {
                allEntries = toc.Where(item => toLoad.Contains(item.Key)).OrderBy(e => e.Value.Offset).Select(e => e.Key);
            }
            total = allEntries.Count();
            Mod.Log($"Loading {total} Blueprints");
            _blueprints = new(total);
            _blueprints.AddRange(Enumerable.Repeat<SimpleBlueprint>(null, total));
            var memStream = new MemoryStream();
            lock (bpCache.m_Lock) {
                bpCache.m_PackFile.Position = 0;
                bpCache.m_PackFile.CopyTo(memStream);
            }
            var chunks = allEntries.Select((entry, index) => (entry, index)).Chunk(Main.Settings.BlueprintsLoaderChunkSize);
            _chunkQueue = new(chunks);
            var bytes = memStream.GetBuffer();
            for (int i = 0; i < Main.Settings.BlueprintsLoaderNumThreads; i++) {
                var t = Task.Run(() => HandleChunks(bytes));
                _workerTasks.Add(t);
            }
            Task.Run(Progressor);
            foreach (var task in _workerTasks) {
                task.Wait();
            }
            _blueprints.RemoveAll(b => b is null);
            watch.Stop();
            Mod.Log($"Threaded loaded roughly {_blueprints.Count + bpsToAdd.Count + BlueprintLoaderPatches.BlueprintsCache_Patches.IsLoading.Value.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            toLoad = null;
            lock (loader) {
                _callback(_blueprints);
                IsRunning = false;
            }
        }
        public void HandleChunks(byte[] bytes) {
            try {
                Stream stream = new MemoryStream(bytes);
                stream.Position = 0;
                var seralizer = new ReflectionBasedSerializer(new PrimitiveSerializer(new BinaryReader(stream), UnityObjectConverter.AssetList));
                int closeCountLocal = 0;
                while (_chunkQueue.TryDequeue(out var entries)) {
                    if (closeCountLocal > 250) {
                        lock (_blueprints) {
                            closeCount += closeCountLocal;
                        }
                        closeCountLocal = 0;
                    }
                    foreach (var entryPairA in entries) {
                        var guid = entryPairA.Item1;
                        try {
                            Object @lock = new();
                            lock (@lock) {
                                int shardIndex = Math.Abs(guid.GetHashCode()) % Main.Settings.BlueprintsLoaderNumShards;
                                var startedLoading = _startedLoadingShards[shardIndex];
                                if (!startedLoading.TryAdd(guid, @lock)) continue;
                                if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.TryGetValue(guid, out var entry)) {
                                    if (entry.Blueprint != null) {
                                        closeCountLocal++;
                                        _blueprints[entryPairA.Item2] = entry.Blueprint;
                                        continue;
                                    }
                                } else {
                                    continue;
                                }
                                if (Shared.BadBlueprints.Contains(guid.ToString()) || entry.Offset == 0U) continue;
                                OnBeforeBPLoad(guid);
                                stream.Seek(entry.Offset, SeekOrigin.Begin);
                                SimpleBlueprint simpleBlueprint = null;
                                seralizer.Blueprint(ref simpleBlueprint);
                                if (simpleBlueprint == null) {
                                    closeCountLocal++;
                                    continue;
                                }
                                object obj = ResourcesLibrary.BlueprintsCache.m_resourceReplacementProvider?.OnResourceLoaded(simpleBlueprint, guid) ?? null;
                                if (obj != null) {
                                    simpleBlueprint = (obj as SimpleBlueprint) ?? simpleBlueprint;
                                }
                                entry.Blueprint = simpleBlueprint;
                                simpleBlueprint.OnEnable();
                                _blueprints[entryPairA.Item2] = simpleBlueprint;
                                ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[guid] = entry;
                                closeCountLocal++;
                                OnAfterBPLoad(guid);
                            }
                        } catch (Exception ex) {
                            Mod.Warn($"Exception loading blueprint {guid}:\n{ex}");
                            closeCountLocal++;
                        }
                    }
                }
            } catch (Exception ex) {
                Mod.Error($"Exception loading blueprints:\n{ex}");
            }
        }
        // These methods exist to allow external mods some interfacing since the bp load bypasses the regular BlueprintsCache.Load.
        // Not using delegate since those would have problems with reloading during runtime.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OnBeforeBPLoad(string bp) {

        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OnAfterBPLoad(string bp) {

        }
        public void Progressor() {
            while (loader.IsRunning) {
                progress = closeCount / (float)total;
                progress = Math.Min(Math.Max(progress, 0), 1);
                Thread.Sleep(200);
            }
        }
        [HarmonyPatch]
        internal static class BlueprintLoaderPatches {
            [HarmonyPatch(typeof(BlueprintsCache))]
            internal static class BlueprintsCache_Patches {
                [HarmonyPatch(nameof(BlueprintsCache.AddCachedBlueprint)), HarmonyPostfix]
                internal static void AddCachedBlueprint(string guid, SimpleBlueprint bp) {
                    if (Shared.IsLoading || Shared.blueprints != null) {
                        lock (Shared.bpsToAdd) {
                            Shared.bpsToAdd.Add(bp);
                        }
                    }
                    if (Shared.IsRunning) {
                        int shardIndex = Math.Abs(guid.GetHashCode()) % Main.Settings.BlueprintsLoaderNumShards;
                        Shared._startedLoadingShards[shardIndex].TryAdd(guid, Shared);
                    }
                }
                [HarmonyPatch(nameof(BlueprintsCache.RemoveCachedBlueprint)), HarmonyPostfix]
                internal static void RemoveCachedBlueprint(string guid) {
                    lock (Shared.bpsToAdd) {
                        Shared.bpsToAdd.RemoveWhere(bp => bp.AssetGuid == guid);
                    }
                }
                internal static ThreadLocal<HashSet<string>> IsLoading = new(() => []);
                [HarmonyPatch(nameof(BlueprintsCache.Load)), HarmonyPrefix]
                public static bool Pre_Load(string guid, ref SimpleBlueprint __result) {
                    if (!Shared.IsRunning) return true;
                    int shardIndex = Math.Abs(guid.GetHashCode()) % Main.Settings.BlueprintsLoaderNumShards;
                    var startedLoading = Shared._startedLoadingShards[shardIndex];
                    if (startedLoading.TryAdd(guid, Shared)) {
                        IsLoading.Value.Add(guid);
                        return true;
                    }
                    lock (startedLoading[guid]) {
                        if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.TryGetValue(guid, out var entry)) {
                            __result = entry.Blueprint;
                        } else {
                            __result = null;
                        }
                    }
                    return false;
                }
                [HarmonyPatch(nameof(BlueprintsCache.Load)), HarmonyPostfix]
                public static void Post_Load(string guid, ref SimpleBlueprint __result) {
                    if (IsLoading.Value.Remove(guid)) {
                        lock (Shared.bpsToAdd) {
                            if (__result != null) Shared.bpsToAdd.Add(__result);
                        }
                    }
                }
            }
            [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init)), HarmonyFinalizer]
            internal static void BlueprintsCache_Init_Patch() {
                Shared.CanStart = true;
                if (Main.Settings.togglePreloadBlueprints || (Main.Settings.toggleUseBPIdCache && Main.Settings.toggleAutomaticallyBuildBPIdCache && BlueprintIdCache.NeedsCacheRebuilt)) Shared.GetBlueprints();
            }
        }
    }
}

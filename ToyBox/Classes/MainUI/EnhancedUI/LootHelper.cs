using Kingmaker;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Interfaces;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using ModKit;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utility;
using Owlcat.Runtime.UI.Controls.Button;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox {
    public static class LootHelper {
        public static string NameAndOwner(this ItemEntity u, bool showRating, bool darkmode = false) {
            var ret = (showRating ? $"{u.Rating()} ".Orange().Bold() : "");
            try {
                ret += (u.Owner != null ? $"({u.Owner.Name}) ".Orange() : "");
            } catch (Exception e) {
                Mod.Error(e.ToString());
            }
            ret += (darkmode ? u.Name.StripHTML().DarkModeRarity(u.Rarity()) : u.Name);
            return ret;
        }
        public static string NameAndOwner(this ItemEntity u, bool darkmode = false) => u.NameAndOwner(Main.Settings.showRatingForEnchantmentInventoryItems, darkmode);
        public static bool IsLootable(this ItemEntity item, RarityType filter = RarityType.None) {
            var rarity = item.Rarity();
            if ((int)rarity < (int)filter) return false;
            return item.IsLootable;
        }
        public static List<ItemEntity> Lootable(this List<ItemEntity> loots, RarityType filter = RarityType.None) => loots.Where(l => l.IsLootable(filter)).ToList();
        public static string GetName(this LootWrapper present) {
            if (present.InteractionLoot != null) {
                //                var name = present.InteractionLoot.Owner.View.name;
                var name = present.InteractionLoot.Source.ToString();
                if (name == null || name.Length == 0) name = "Ground";
                return name;
            }
            if (present.Unit != null) return present.Unit.CharacterName;
            return null;
        }

        public static List<ItemEntity> GetInteraction(this LootWrapper present) {
            if (present.InteractionLoot != null) return present.InteractionLoot.Loot.Items
                                                               .ToList()
                                                               ;
            if (present.Unit != null) return present.Unit.Inventory.Items
                                                    .ToList()
                                                    ;
            return null;
        }
        public static IEnumerable<ItemEntity> Search(this IEnumerable<ItemEntity> items, string searchText) => items.Where(i => searchText.Length > 0 ? i.Name.ToLower().Contains(searchText.ToLower()) : true);
        public static List<ItemEntity> GetLewtz(this LootWrapper present, string searchText = "") {
            if (present.InteractionLoot != null) return present.InteractionLoot.Loot.Items.Search(searchText).ToList();
            if (present.Unit != null) return present.Unit.Inventory.Items.Search(searchText).ToList();
            return null;
        }
        // TODO: implement ToyBox improvements
        public static IEnumerable<LootWrapper> GetMassLootFromCurrentArea() {
            var lootFromCurrentArea = new List<LootWrapper>();
            foreach (var baseUnitEntity in Shodan.AllBaseUnits.Where(u => u.IsRevealed && u.IsDeadAndHasLoot))
                lootFromCurrentArea.Add(new LootWrapper {
                    Unit = baseUnitEntity
                });
            var interactionLootParts = Game.Instance.State.MapObjects.Select(i => i.GetOptional<InteractionLootPart>())
                                           .Concat(Game.Instance.State.AllUnits.Select(i => i.GetOptional<InteractionLootPart>())).NotNull();
            var source = TempList.Get<InteractionLootPart>();
            foreach (var interactionLootPart in interactionLootParts)
                if ((interactionLootPart.Owner.IsRevealed || Main.Settings.toggleShowHiddenLoot) && interactionLootPart.Loot.HasLoot &&
                    (interactionLootPart.LootViewed || Main.Settings.toggleShowHiddenLoot ||
                     (interactionLootPart.View is DroppedLoot && !(bool)(EntityPart)interactionLootPart.Owner
                                                                                                       .GetOptional<DroppedLoot.EntityPartBreathOfMoney>()) ||
                     (bool)(UnityEngine.Object)interactionLootPart.View.GetComponent<SkinnedMeshRenderer>()))
                    source.Add(interactionLootPart);
            var collection = source.Distinct(new MassLootHelper.LootDuplicateCheck()).Select(i => new LootWrapper {
                InteractionLoot = i
            });
            lootFromCurrentArea.AddRange(collection);
            return lootFromCurrentArea;
        }
        public static void OpenMassLoot() {
            var loot = MassLootHelper.GetMassLootFromCurrentArea();
            if (loot == null) return;
            var count = loot.Count();
            var count2 = loot.Count(present => present.InteractionLoot != null);
            Mod.Debug($"MassLoot: Count = {loot.Count()}");
            Mod.Debug($"MassLoot: Count2 = {count}");
            if (count == 0) return;
            // Access to LootContextVM
            var contextVM = RootUIContext.Instance
                                         .SurfaceVM?
                                         .StaticPartVM?.LootContextVM;
            if (contextVM == null) return;
            // Add new loot...
            var lootVM = new LootVM(LootContextVM.LootWindowMode.ZoneExit, loot, null, () => contextVM.DisposeAndRemove(contextVM.LootVM));

            // Open window add lootVM int contextVM
            contextVM.LootVM.Value = lootVM;

            //EventBus.RaiseEvent((Action<ILootInterractionHandler>)(e => e.HandleZoneLootInterraction(null)));
            UnityModManager.UI.Instance.ToggleWindow(false);
        }
        private static DroppedLoot m_FakePlayerChest;
        // TODO: Verify this actually works
        public static void OpenPlayerChest() {
            var contextVM = RootUIContext.Instance.SurfaceVM?.StaticPartVM?.LootContextVM;
            if (contextVM == null) return;
            if (m_FakePlayerChest == null) {
                var wow = new GameObject("._.");
                // Need to disable the GO to prevent DroppedLoot.OnEnable from running since that causes an NRE that can't be caught properly
                wow.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(wow);
                var a = wow.EnsureComponent<DroppedLoot>();
                var il = wow.EnsureComponent<InteractionLoot>();
                il.Settings = new() { LootContainerType = LootContainerType.PlayerChest, 
                    PutItemTrigger = new() { Action = new() { guid = null } }, 
                    CloseTrigger = new() { Action = new() { guid = null } }, 
                    TakeItemTrigger = new() { Action = new() { guid = null } } };
                ((EntityViewBase)a).Data = Entity.Initialize(new DroppedLoot.EntityData(a));
                var ilp = a.Data.ToEntity().GetOrCreate<InteractionLootPart>();
                a.Data.AttachView(a);
                ilp.SetSettings(il.Settings);
                wow.SetActive(true);
                m_FakePlayerChest = a;
            }
            m_FakePlayerChest.Data.ToEntity().GetOptional<InteractionLootPart>().Loot = Game.Instance.Player.SharedStash;
            var lootVM = new LootVM(LootContextVM.LootWindowMode.PlayerChest, [m_FakePlayerChest], () => contextVM.DisposeAndRemove(contextVM.LootVM));

            contextVM.LootVM.Value = lootVM;
        }
    }
}
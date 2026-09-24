using Kingmaker;
using Kingmaker.UI.MVVM.View.ServiceWindows.CharacterInfo.Sections.Careers.PC.CareerPathProgression.SelectionTabs;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.CareerPath;
using Kingmaker.UI.MVVM.VM.ServiceWindows.CharacterInfo.Sections.Careers.RankEntry;
using Kingmaker.UnitLogic.Levelup;
using Kingmaker.UnitLogic.Levelup.Selections;
using Kingmaker.UnitLogic.Levelup.Selections.Feature;
using System.Runtime.CompilerServices;
using UniRx;

namespace ToyBox.Features.LevelUp;

/// <summary>
/// Optional-selections navigation and auto-advance, running in the selection
/// multiplier's Harmony category (enabled/disabled together with it).
/// - Exhausted pools pass the Finish gate: an unmade selection whose entire
///   candidate pool fails CanSelect follows the game's own natively-empty
///   rule instead of dead-ending the level-up (multiplied groups can exhaust
///   a pool by filling sibling slots).
/// - Make Selections Optional (Level Up tab): every unmade selection passes
///   the gate, letting the user deliberately skip unwanted options. Next
///   skips the current page forward; Finish stays on the last page or when
///   everything unmade is exhausted.
/// - Auto-advance: after a pick lands on a multiplied row, the shown page
///   moves to the next unmade sibling occurrence. Vanilla keeps the page on
///   the already-selected row, so with k multiplied rows every further click
///   silently overwrites the same slot.
/// Note: methods of generic type definitions cannot be Harmony-patched — this
/// file only patches methods declared on the concrete view classes; calling
/// inherited base members is fine.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.LevelUp.TalentSelectionMultiplierFeature")]
internal static class LevelUpNavigationPatch {
    private static readonly HashSet<string> m_WarnedSites = [];

    private static void WarnOnce(string site, Exception ex) {
        if (m_WarnedSites.Add(site)) {
            Warn($"LevelUpNavigationPatch[{site}]: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Rows deliberately skipped under Make Selections Optional. The render
    // pass treats a skipped unmade row as visited so later ranks stop waiting
    // on it.
    private static readonly ConditionalWeakTable<RankEntrySelectionVM, object> s_SkippedRows = new();

    internal static void MarkRowSkipped(RankEntrySelectionVM row) {
        try {
            if (row != null) {
                _ = s_SkippedRows.GetValue(row, _ => new object());
            }
        } catch {
            // Must never throw into game code.
        }
    }

    // Skip marks are per-SESSION state. The career view builds its rows once
    // (InitializeRankEntries only fills an empty RankEntries) and rebinds the
    // same row objects to each new LevelUpManager, so when UpdateRanks —
    // reached on session create, commit, cancel and view rebind — observes a
    // different manager, the previous session's marks must be released: a row
    // skipped in one session would otherwise render forced NotSelectable
    // forever in the next (the skip-render ignores the toggle), and with
    // Make Selections Optional off again the unmade-but-marked row also
    // dead-ends the vanilla Finish gate.
    private static readonly ConditionalWeakTable<CareerPathVM, StrongBox<LevelUpManager?>> s_LastManager = new();

    [HarmonyPatch(typeof(CareerPathVM), nameof(CareerPathVM.UpdateRanks)), HarmonyPrefix]
    private static void CareerPathVM_UpdateRanks_SessionReset_Prefix(CareerPathVM __instance) {
        try {
            var managerBox = s_LastManager.GetValue(__instance, _ => new StrongBox<LevelUpManager?>());
            var boundManager = __instance.UnitProgressionVM?.LevelUpManager;
            if (ReferenceEquals(managerBox.Value, boundManager)) {
                return;
            }
            managerBox.Value = boundManager;
            foreach (var row in __instance.AllSelections) {
                _ = s_SkippedRows.Remove(row);
            }
        } catch (Exception ex) {
            WarnOnce("session-reset", ex);
        }
    }

    // The next candidate page for the pick auto-advance: the first row AFTER
    // `picked` (UI scan order) that shares its (blueprint, rank), is still
    // unmade and still offers a selectable candidate. Exhausted siblings are
    // skipped (advancing onto a natively-empty page would strand the user on
    // a blank slot), and so are different blueprints and different ranks, so
    // single-row (vanilla) pages keep their stay-on-page behavior. A skip
    // mark does NOT disqualify a sibling: the mark only forces the render
    // while the row stays unmade, and a live pool means the user can still
    // land there.
    internal static RankEntrySelectionVM? NextUnmadeSibling(CareerPathVM career, RankEntrySelectionVM picked) {
        var state = picked.m_SelectionStateFeature.Value;
        if (state == null) {
            return null; // Orphan/cleared row: nothing to advance from.
        }
        var pickedIndex = career.RankEntriesScan.IndexOf(picked);
        if (pickedIndex < 0) {
            return null; // A row absent from the scan must not wrap around to earlier siblings.
        }
        return career.RankEntriesScan
            .Skip(pickedIndex + 1)
            .OfType<RankEntrySelectionVM>()
            .Where(row => row.Rank == picked.Rank)
            .FirstOrDefault(row => {
                var rowState = row.m_SelectionStateFeature.Value;
                return rowState != null
                    && rowState.Blueprint == state.Blueprint
                    && !row.SelectionMade
                    && rowState.CanSelectAny;
            });
    }

    // True when the attempted pick actually landed on this state. Select
    // either sets SelectionItem to exactly the passed item or (rejected
    // re-pick) leaves the PRIOR pick in place, so HasValue alone cannot
    // distinguish a landed pick from a stale one.
    internal static bool PickLandedOnState(SelectionStateFeature? state, FeatureSelectionItem? picked) =>
        state != null
        && picked.HasValue
        && state.SelectionItem.HasValue
        && state.SelectionItem.Value.Feature == picked.Value.Feature;

    // Every fire-time guard of the deferred auto-advance in one predicate;
    // the final clause closes the fast-click race (a second pick that landed
    // within the delay replaced the captured pick on the same occurrence, so
    // this timer stands down and the newer pick's own timer carries the
    // advance).
    internal static bool ShouldRunDeferredAutoAdvance(CareerPathVM career, RankEntrySelectionVM picked, RankEntrySelectionVM next, FeatureSelectionItem? capturedPick) {
        if (career.LevelUpManager == null) { return false; } // session ended during the delay
        if (career.RankEntries.Count == 0) { return false; } // CareerPathVM disposed
        if (!career.RankEntriesScan.Contains(next)) { return false; } // row removed since scheduling
        if (career.UnitProgressionVM?.CurrentRankEntryItem.Value != picked) { return false; } // user navigated away
        return PickLandedOnState(picked.m_SelectionStateFeature.Value, capturedPick); // fast-click re-check
    }

    // Two relaxation modes on IsAllSelectionsMadeAndValid:
    // (a) Exhausted pools: unmade states whose entire candidate pool fails
    //     CanSelect pass automatically (the game's own natively-empty rule).
    // (b) Optional selections: when the toggle is on, ALL unmade states pass,
    //     letting the user finish with empty slots they deliberately chose
    //     not to fill.
    [HarmonyPatch(typeof(LevelUpManager), nameof(LevelUpManager.IsAllSelectionsMadeAndValid), MethodType.Getter), HarmonyPostfix]
    private static void LevelUpManager_IsAllSelectionsMadeAndValid_Postfix(
        LevelUpManager __instance,
        ref bool __result) {
        if (__result) {
            return;
        }
        try {
            // Mode (b): optional selections — everything passes, but only for
            // sessions on an already-recruited unit (party member/pet or the
            // player's ship). Origin chargen and mercenary recruitment keep
            // their mandatory picks.
            if (Settings.MakeSelectionsOptional) {
                if (ShouldForceCompletePhase(__instance)) {
                    __result = true;
                }
                return;
            }
            // Mode (a): exhausted pools only. A made-but-invalid pick still
            // blocks, matching vanilla intent — the user did choose
            // something; it broke, and silently forcing the gate open would
            // drop the pick at replay instead of making them fix it.
            if (__instance.Selections.OfType<SelectionStateFeature>().Any(s => s.IsMade && !s.IsValid)) {
                return;
            }
            foreach (SelectionState selection in __instance.Selections) {
                if (selection is not SelectionStateFeature state) {
                    // Non-feature selections (e.g. chargen picks) follow the
                    // vanilla rule: block only while they still offer a
                    // selectable item - the same CanSelectAny clause
                    // AllUnmadeSelectionsExhausted uses.
                    if (!selection.IsMade && selection.CanSelectAny) {
                        return;
                    }
                    continue;
                }
                if (state.IsMade) {
                    continue;
                }
                if (state.Items.All(item => !state.CanSelect(item))) {
                    continue;
                }
                return; // an unmade selection still has selectable candidates
            }
            __result = true;
        } catch {
            // The vanilla result stands.
        }
    }

    internal static bool ShouldForceFinishInteractable(bool canCommit, bool isLastPage, bool allUnmadeExhausted) {
        return canCommit && (allUnmadeExhausted || (Settings.MakeSelectionsOptional && isLastPage));
    }

    // "Last page" is whatever item the game marked LastEntryToUpgrade — for
    // multi-rank jumps that is often a FEATURE page (RankEntryFeatureItemVM),
    // not a selection page, so identity is compared against
    // IRankEntrySelectItem.
    internal static bool IsLastSelectionPage(IRankEntrySelectItem? lastEntryToUpgrade, IRankEntrySelectItem? currentVm) {
        return ReferenceEquals(lastEntryToUpgrade, currentVm);
    }

    // Next under the optional toggle: skipping the current page forward is
    // allowed whenever the session is commit-valid and a later page exists.
    internal static bool ShouldForceNextInteractable(bool canCommit, bool hasNextPage) {
        return canCommit && Settings.MakeSelectionsOptional && hasNextPage;
    }

    // A page the unlocked Next skips must leave FeaturesToVisit (vanilla's
    // "visited == selected" invariant no longer holds) or the pointer drags
    // the window back to it after every later pick.
    internal static bool ShouldMarkSkipVisited(bool selectionMade, bool inVisitList) {
        return Settings.MakeSelectionsOptional && !selectionMade && inVisitList;
    }

    // PetKeystone selections are NOT skippable: CareerPathVM.Commit() silently
    // returns when a PetKeystone selection has no pick, so an unlocked
    // skip/Finish would strand the user on a lit-but-dead button.
    internal static bool IsSkippableSelectionGroup(FeatureGroup group) {
        return group != FeatureGroup.PetKeystone;
    }

    internal static bool HasUnmadePetKeystoneSelection(IEnumerable<RankEntrySelectionVM>? available) {
        return available != null
            && available.Any(s => s is { SelectionMade: false, FeatureGroup: FeatureGroup.PetKeystone });
    }

    // True when no unmade selection in the session still offers a selectable
    // item (made picks and natively-empty slots don't count against it).
    // Non-feature states (e.g. ship selections) count against exhaustion the
    // same way via the shared CanSelectAny rule.
    internal static bool AllUnmadeSelectionsExhausted(LevelUpManager? manager) {
        if (manager == null) {
            return false;
        }
        foreach (SelectionState selection in manager.Selections) {
            if (selection is not SelectionStateFeature state) {
                if (!selection.IsMade && selection.CanSelectAny) {
                    return false;
                }
                continue;
            }
            if (state.IsMade) {
                continue;
            }
            if (!state.Items.All(item => !state.CanSelect(item))) {
                return false;
            }
        }
        return true;
    }

    // Sessions on a unit that is ALREADY the player's: a recruited roster
    // character or the player's starship. Origin chargen runs on a throwaway
    // blueprint unit and mercenary recruitment runs on a not-yet-recruited
    // unit — both must keep their mandatory rank-0 picks, or the built
    // character would be broken.
    // Membership is checked by entity reference, NOT via IsPartyOrPet's
    // blueprint match: every custom mercenary is created from the shared
    // CustomCompanion blueprint (Player.CreateCustomCompanion), so once one
    // merc is recruited the blueprint match would classify the NEXT merc's
    // creation session as "already the player's" and force its finish gate
    // open with mandatory picks unmade. TargetUnit is the real session
    // entity (the game itself compares it by reference, e.g.
    // UnitProgressionVM.TryGetActiveLevelupCareer), and the AllCharacters
    // getter refreshes the roster, so Contains is exact.
    internal static bool ShouldForceCompletePhase(LevelUpManager? manager) {
        if (manager == null || manager.AutoCommit) {
            return false;
        }
        var unit = manager.TargetUnit;
        return ToyBoxUnitHelper.IsPlayerStarship(unit)
            || Game.Instance?.Player?.AllCharacters.Contains(unit) == true;
    }

    // Deliberately-skipped rows (optional-skip Next) would hold the
    // FirstSelectable position forever - vanilla's rank gating then leaves
    // every later rank's rows in WaitPreviousToSelect, rendering all
    // subsequent pages locked/empty. A skipped row renders as NotSelectable
    // (the natively-empty state), which NeedToSelect/FirstSelectable skip
    // over, freeing the later ranks. The forced render applies ONLY while
    // the row is truly unmade: a pick that lands on a skipped row must
    // render normally, so the mark is released instead.
    [HarmonyPatch(typeof(RankEntrySelectionVM), nameof(RankEntrySelectionVM.UpdateState)), HarmonyPostfix]
    private static void RankEntrySelectionVM_UpdateState_SkipRender_Postfix(RankEntrySelectionVM __instance) {
        try {
            if (!s_SkippedRows.TryGetValue(__instance, out _)) {
                return;
            }
            if (__instance.m_SelectionStateFeature.Value?.SelectionItem.HasValue == true) {
                _ = s_SkippedRows.Remove(__instance);
                return;
            }
            __instance.EntryState.Value = RankEntryState.NotSelectable;
            __instance.SetSelectedFeature((FeatureSelectionItem?)null);
        } catch (Exception ex) {
            WarnOnce("skip-render", ex);
        }
    }

    #region View patches
    [HarmonyPatch(typeof(RankEntryFeatureSelectionPCView), nameof(RankEntryFeatureSelectionPCView.BindViewImplementation)), HarmonyPostfix]
    private static void FeatureSelection_BindViewImplementation_Postfix(RankEntryFeatureSelectionPCView __instance) {
        try {
            var viewModel = __instance.ViewModel;
            __instance.AddDisposable(MakeFinishSubscription(viewModel.CareerPathVM, viewModel, enabled => {
                if (enabled) {
                    __instance.SetFinishInteractable(true);
                }
            }));
        } catch (Exception ex) {
            WarnOnce("selection/bind", ex);
        }
    }

    [HarmonyPatch(typeof(RankEntryFeatureSelectionPCView), nameof(RankEntryFeatureSelectionPCView.UpdateState)), HarmonyPostfix]
    private static void FeatureSelection_UpdateState_Postfix(RankEntryFeatureSelectionPCView __instance) {
        try {
            var viewModel = __instance.ViewModel;
            var careerVM = viewModel.CareerPathVM;
            if (IsSkippableSelectionGroup(viewModel.FeatureGroup)
                && ShouldForceNextInteractable(
                    careerVM.CanCommit.Value,
                    hasNextPage: !IsLastSelectionPage(careerVM.LastEntryToUpgrade, viewModel))) {
                __instance.SetNextButtonInteractable(true);
                // Vanilla ties the attention highlight to its own Next gate;
                // don't leave it lit next to an unlocked skip button.
                __instance.m_HighlightButton?.gameObject.SetActive(false);
            }
            RefreshFinish(__instance, careerVM, viewModel);
        } catch (Exception ex) {
            WarnOnce("selection/state", ex);
        }
    }

    // Vanilla only removes a selection page from FeaturesToVisit when its
    // pick is MADE ("visited == selected" invariant; Next required a
    // completed pick). Optional-skip breaks that invariant: a skipped page
    // would sit at the head of the visit list forever, and after ANY later
    // pick the pointer (FeaturesToVisit.FirstOrDefault) drags the window
    // back to it. Mark the page visited at the moment the unlocked Next
    // skips it.
    [HarmonyPatch(typeof(RankEntryFeatureSelectionPCView), nameof(RankEntryFeatureSelectionPCView.HandleClickNext)), HarmonyPrefix]
    private static void FeatureSelection_HandleClickNext_Prefix(RankEntryFeatureSelectionPCView __instance) {
        try {
            var viewModel = __instance.ViewModel;
            if (IsSkippableSelectionGroup(viewModel.FeatureGroup)
                && ShouldMarkSkipVisited(
                    viewModel.SelectionMade,
                    viewModel.CareerPathVM.FeaturesToVisit.Contains(viewModel))) {
                viewModel.CareerPathVM.FeaturesToVisit.Remove(viewModel);
                // Also mark the row skipped so FirstSelectable moves past it
                // and later ranks stop waiting on it.
                MarkRowSkipped(viewModel);
            }
        } catch (Exception ex) {
            WarnOnce("selection/nextclick", ex);
        }
    }

    [HarmonyPatch(typeof(RankEntryFeatureDescriptionPCView), nameof(RankEntryFeatureDescriptionPCView.BindViewImplementation)), HarmonyPostfix]
    private static void FeatureDescription_BindViewImplementation_Postfix(RankEntryFeatureDescriptionPCView __instance) {
        try {
            var viewModel = __instance.ViewModel;
            __instance.AddDisposable(MakeFinishSubscription(viewModel.CareerPathVM, viewModel, enabled => {
                if (enabled) {
                    __instance.SetFinishInteractable(true);
                }
            }));
        } catch (Exception ex) {
            WarnOnce("feature/bind", ex);
        }
    }

    [HarmonyPatch(typeof(RankEntryFeatureDescriptionPCView), nameof(RankEntryFeatureDescriptionPCView.UpdateState)), HarmonyPostfix]
    private static void FeatureDescription_UpdateState_Postfix(RankEntryFeatureDescriptionPCView __instance) {
        try {
            var viewModel = __instance.ViewModel;
            RefreshFinish(__instance, viewModel.CareerPathVM, viewModel);
        } catch (Exception ex) {
            WarnOnce("feature/state", ex);
        }
    }

    // Finish may light on EITHER tab view (selection pages AND feature
    // pages — the last page of a multi-rank jump is often a feature page),
    // evaluated both on UpdateState (exhaustion can flip without a
    // CanCommit emission) and via a later-running subscription after
    // vanilla's own.
    private static void RefreshFinish<TVm>(BaseCareerPathSelectionTabPCView<TVm> view, CareerPathVM careerVM, IRankEntrySelectItem currentVm) where TVm : class, Owlcat.Runtime.UI.MVVM.IViewModel {
        if (!HasUnmadePetKeystoneSelection(careerVM.AvailableSelections)
            && ShouldForceFinishInteractable(
                careerVM.CanCommit.Value,
                IsLastSelectionPage(careerVM.LastEntryToUpgrade, currentVm),
                AllUnmadeSelectionsExhausted(careerVM.LevelUpManager))) {
            view.SetFinishInteractable(true);
        }
    }

    private static IDisposable MakeFinishSubscription(CareerPathVM careerVM, IRankEntrySelectItem currentVm, Action<bool> applyFinish) {
        return careerVM.CanCommit.CombineLatest(careerVM.PointerItem,
            (canCommit, _) => {
                try {
                    return !HasUnmadePetKeystoneSelection(careerVM.AvailableSelections)
                        && ShouldForceFinishInteractable(
                            canCommit,
                            IsLastSelectionPage(careerVM.LastEntryToUpgrade, currentVm),
                            AllUnmadeSelectionsExhausted(careerVM.LevelUpManager));
                } catch {
                    // The selector runs inside future vanilla emissions;
                    // never let it throw into the game's chain.
                    return false;
                }
            })
            .Subscribe(applyFinish);
    }
    #endregion

    #region Auto-advance
    // After a successful pick on a multiplied row, auto-advance the shown
    // page to the next unmade sibling occurrence (see class docs). Only
    // advances to a same-(blueprint, rank) unmade sibling, so single-row
    // (vanilla) pages keep their stay-on-page behavior.
    [HarmonyPatch(typeof(RankEntrySelectionVM), nameof(RankEntrySelectionVM.SelectFeature)), HarmonyPostfix]
    private static void SelectionVM_SelectFeature_AutoAdvance_Postfix(
        RankEntrySelectionVM __instance,
        FeatureSelectionItem? featureSelectionItem) {
        try {
            if (!featureSelectionItem.HasValue) {
                return; // A clear, not a pick.
            }
            var state = __instance.m_SelectionStateFeature.Value;
            if (!PickLandedOnState(state, featureSelectionItem)) {
                return; // Pick did not land (cleared, or rejected re-pick left the old pick).
            }
            var career = __instance.CareerPathVM;
            var next = NextUnmadeSibling(career, __instance);
            if (next != null) {
                // Defer one beat so the pick's own post-processing finishes
                // first (preview-unit rebuild, candidate re-sort), and pin
                // the timer to the main thread so SetRankEntry never runs on
                // a thread-pool worker.
                Observable.Timer(TimeSpan.FromMilliseconds(200), Scheduler.MainThread)
                    .Subscribe(_ => {
                        try {
                            if (ShouldRunDeferredAutoAdvance(career, __instance, next, featureSelectionItem)) {
                                career.SetRankEntry(next);
                            }
                        } catch (Exception ex) {
                            WarnOnce("auto-advance", ex);
                        }
                    });
            }
        } catch (Exception ex) {
            WarnOnce("auto-advance", ex);
        }
    }
    #endregion
}

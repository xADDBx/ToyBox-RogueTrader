using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using System.Diagnostics;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Stats;

[IsTested]
public partial class PortraitEditorFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_Name", "Change Unit Portrait")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_Description", "Allows changing the portrait of a unit to a custom portrait or another blueprint (built-in) portrait. You need to manually update the UI (change area/reload game) to make this change visible on the top bar.")]
    public override partial string Description { get; }
    private static bool m_IsSubscribed = false;
    public override void Initialize() {
        base.Initialize();
        if (!m_IsSubscribed) {
            Main.OnHideGUIAction += UnloadPortraits;
            m_IsSubscribed = true;
        }
    }
    public override void Destroy() {
        base.Destroy();
        if (m_IsSubscribed) {
            Main.OnHideGUIAction -= UnloadPortraits;
            m_IsSubscribed = false;
        }
        UnloadPortraits();
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }

    public void OnGui(BaseUnitEntity unit) {
        using (VerticalScope()) {
            if (unit.UISettings.Portrait.IsCustom) {
                UI.Label(m_CurrentCustomPortraitLocalizedText + ":");
                OnPortraitGUI(unit.UISettings.Portrait.CustomId, false, 0.25f);
            } else {
                UI.Label(m_CurrentBlueprintPortraitLocalizedText + ":");
                OnPortraitGUI(unit.UISettings.PortraitBlueprint, false, 0.25f, (int)(0.25f * 692));
            }
            CustomPortraitPicker(unit);
            BlueprintPortraitPicker(unit);
        }
    }
    private bool m_ShowCustomPortraitPicker = false;
    private static readonly TimedCache<float> m_NewCustomPortraitLabelWidth = new(() => {
        return CalculateLargestLabelWidth([m_NameOfTheNewCustomPortrait_LocalizedText + " "]);
    });
    private static readonly TimedCache<float> m_NewBlueprintPortraitLabelWidth = new(() => {
        return CalculateLargestLabelWidth([m_NameOfTheNewBlueprintportrait_LocalizedText + " "]);
    });
    private bool m_TriedToPickUnknownId = false;
    private Browser<string>? m_CustomPortraitBrowser;
    public const int PortraitsPerRow = 6;
    private void CustomPortraitPicker(BaseUnitEntity unit) {
        UI.DisclosureToggle(ref m_ShowCustomPortraitPicker, m_ShowCustomPortraitPickerLocalizedText);
        if (m_ShowCustomPortraitPicker) {
            using (HorizontalScope()) {
                UI.Label(m_NameOfTheNewCustomPortrait_LocalizedText, Width(m_NewCustomPortraitLabelWidth));
                UI.TextField(ref m_NewCustomPortraitName, null, GUILayout.MinWidth(Main.UIScale * 200), AutoWidth());
                if (UI.Button(m_ChangePortraitLocalizedText)) {
                    if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds().Contains(m_NewCustomPortraitName)) {
                        unit.UISettings.SetPortrait(new PortraitData(m_NewCustomPortraitName));
                        Debug($"Changed portrait of {unit.CharacterName} to {m_NewCustomPortraitName}");
                        m_TriedToPickUnknownId = false;
                    } else {
                        Warn($"No portrait with name {m_NewCustomPortraitName}");
                        m_TriedToPickUnknownId = true;
                    }
                }
                if (m_TriedToPickUnknownId) {
                    Space(25);
                    UI.Label(m_UnknownID_LocalizedText.Red());
                }
            }
            if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds() is string[] customIDs) {
                if (m_CustomPortraitBrowser == null) {
                    m_CustomPortraitBrowser = new(id => id, id => id, customIDs, showDivBetweenItems:false, overridePageLimit: 18, overridePageWidth: (int)(EffectiveWindowWidth() * 0.9f));
                    m_LoadedCustomPortraits = true;
                }
                var currentIndex = 0;
                var targetWidth = (int)(692 * EffectiveWindowWidth() * 0.9f / (PortraitsPerRow * 780));
                m_CustomPortraitBrowser.OnGUI(id => {
                    if (currentIndex % PortraitsPerRow == 0) {
                        GUILayout.BeginHorizontal();
                    }
                    currentIndex++;
                    OnPortraitGUI(id, true, EffectiveWindowWidth() * 0.9f / (PortraitsPerRow * 780));
                    if (currentIndex % PortraitsPerRow == 0 || m_CustomPortraitBrowser.CurrentlyIsLastElement) {
                        if (m_CustomPortraitBrowser.CurrentlyIsLastElement) {
                            while (currentIndex % PortraitsPerRow != 0) {
                                currentIndex++;
                                UnscaledSpace(targetWidth);
                            }
                        }
                        GUILayout.EndHorizontal();
                        if (!m_CustomPortraitBrowser.CurrentlyIsLastElement) {
                            Div.DrawDiv();
                        }
                        Space(2);
                    }
                });
            }
        }
    }
    private bool m_ShowBlueprintPortraitPicker = false;
    private Browser<BlueprintPortrait>? m_BlueprintPortraitBrowser;
    private void BlueprintPortraitPicker(BaseUnitEntity unit) {
        UI.DisclosureToggle(ref m_ShowBlueprintPortraitPicker, m_ShowBlueprintPortraitPickerLocalizedText);
        if (m_ShowBlueprintPortraitPicker) {
            using (HorizontalScope()) {
                UI.Label(m_NameOfTheNewBlueprintportrait_LocalizedText, Width(m_NewBlueprintPortraitLabelWidth));
                if (m_CurrentlyPickedBlueprintPortrait != null) {
                    UI.Label(BPHelper.GetTitle(m_CurrentlyPickedBlueprintPortrait), GUILayout.MinWidth(Main.UIScale * 200), AutoWidth());
                } else {
                    UI.Label("", Width(200 * Main.UIScale));
                }
                if (UI.Button(m_ChangePortraitLocalizedText)) {
                    if (m_CurrentlyPickedBlueprintPortrait != null) {
                        unit.UISettings.SetPortrait(m_CurrentlyPickedBlueprintPortrait);
                        Debug($"Changed portrait of {unit.CharacterName} to {BPHelper.GetTitle(m_CurrentlyPickedBlueprintPortrait)}");
                    }
                };
            }
            if (m_BlueprintPortraitBrowser == null) {
                m_BlueprintPortraitBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null,
                    func => BPLoader.GetBlueprintsOfType((IEnumerable<BlueprintPortrait> bps) => {
                        func(bps.Where(bp => bp.FullLengthPortrait != null));
                    }), showDivBetweenItems: false, overridePageLimit: 18, overridePageWidth: (int)(EffectiveWindowWidth() * 0.9f), hideShowAllEvenWithFunc: true);
                m_BlueprintPortraitBrowser.ForceShowAll();
            }
            var currentIndex = 0;
            var targetWidth = (int)(692 * EffectiveWindowWidth() * 0.9f / (PortraitsPerRow * 780));
            m_BlueprintPortraitBrowser.OnGUI(bp => {
                if (currentIndex % PortraitsPerRow == 0) {
                    GUILayout.BeginHorizontal();
                }
                currentIndex++;
                OnPortraitGUI(bp, true, 0.5f, targetWidth);
                if (currentIndex % PortraitsPerRow == 0 || m_BlueprintPortraitBrowser.CurrentlyIsLastElement) {
                    if (m_BlueprintPortraitBrowser.CurrentlyIsLastElement) {
                        while (currentIndex % PortraitsPerRow != 0) {
                            currentIndex++;
                            UnscaledSpace(targetWidth);
                        }
                    }
                    GUILayout.EndHorizontal();
                    if (!m_BlueprintPortraitBrowser.CurrentlyIsLastElement) {
                        Div.DrawDiv();
                    }
                    Space(2);
                }
            });
        }
    }
    private void UnloadPortraits() {
        if (m_LoadedCustomPortraits) {
            m_LoadedCustomPortraits = false;
            m_PortraitsByID.Clear();
            CustomPortraitsManager.Instance.Cleanup();
            m_CustomPortraitBrowser = null;
        }
    }
    private static bool m_LoadedCustomPortraits = false;
    private static readonly Dictionary<string, PortraitData> m_PortraitsByID = [];
    private string m_NewCustomPortraitName = "";
    private BlueprintPortrait? m_CurrentlyPickedBlueprintPortrait;
    public static PortraitData? LoadCustomPortrait(string customID) {
        try {
            if (!m_PortraitsByID.TryGetValue(customID, out var portraitData)) {
                portraitData = new PortraitData(customID);
                if (portraitData.DirectoryExists()) {
                    m_PortraitsByID[customID] = CustomPortraitsManager.CreatePortraitData(customID);
                    return m_PortraitsByID[customID];
                }
            } else {
                return portraitData;
            }
        } catch (Exception ex) {
            Log(ex.ToString());
        }
        return null;
    }
    public void OnPortraitGUI(string customID, bool isButton = true, float scaling = 0.5f, int? targetWidth = null) {
        var portraitData = LoadCustomPortrait(customID);
        if (portraitData != null) {
            var sprite = portraitData.FullLengthPortrait;
            int w, h;
            if (targetWidth.HasValue) {
                w = targetWidth.Value;
                h = (int)(sprite.rect.height * targetWidth / sprite.rect.width);
            } else {
                w = (int)(sprite.rect.width * scaling);
                h = (int)(sprite.rect.height * scaling);
            }
            using (VerticalScope()) {
                if (isButton) {
                    if (GUILayout.Button(sprite.texture, Width(w), Height(h))) {
                        m_NewCustomPortraitName = customID;
                    }
                } else {
                    GUILayout.Label(sprite.texture, GUI.skin.button, Width(w), Height(h));
                }
                UI.Label(customID);
            }
        }
    }
    public void OnPortraitGUI(BlueprintPortrait portrait, bool isButton = true, float scaling = 0.5f, int? targetWidth = null) {
        if (portrait != null) {
            var sprite = portrait.FullLengthPortrait;
            if (sprite == null) {
                return;
            }
            int w, h;
            if (targetWidth.HasValue) {
                w = targetWidth.Value;
                h = (int)(sprite.rect.height * targetWidth / sprite.rect.width);
            } else {
                w = (int)(sprite.rect.width * scaling);
                h = (int)(sprite.rect.height * scaling);
            }
            using (VerticalScope()) {
                if (isButton) {
                    if (GUILayout.Button(sprite.texture, Width(w), Height(h))) {
                        m_CurrentlyPickedBlueprintPortrait = portrait;
                    }
                } else {
                    GUILayout.Label(sprite.texture, GUI.skin.button, Width(w), Height(h));
                }
                UI.Button(m_SavePortraitAsPngLocalizedText, () => {
                    try {
                        var portraitDir = new DirectoryInfo(Path.Combine(Main.ModEntry.Path, "Portraits", portrait.name));
                        if (!portraitDir.Exists) {
                            portraitDir.Create();
                        }
                        var outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitSmallName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                        portrait.SmallPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                        outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitMediumName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                        portrait.HalfLengthPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                        outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitBigName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                        portrait.FullLengthPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                        Process.Start(portraitDir.FullName);
                    } catch (Exception ex) {
                        Error(ex);
                    }
                });
                UI.Label(BPHelper.GetTitle(portrait), GUILayout.MinWidth(200 * Main.UIScale), AutoWidth());
            }
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_CurrentCustomPortraitLocalizedText", "Current Custom Portrait")]
    private static partial string m_CurrentCustomPortraitLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_CurrentBlueprintPortraitLocalizedText", "Current Blueprint Portrait")]
    private static partial string m_CurrentBlueprintPortraitLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_SavePortraitAsPngLocalizedText", "Save portrait as png")]
    private static partial string m_SavePortraitAsPngLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_ShowCustomPortraitPickerLocalizedText", "Show Custom Portrait Picker")]
    private static partial string m_ShowCustomPortraitPickerLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_ShowBlueprintPortraitPickerLocalizedText", "Show Blueprint Portrait Picker")]
    private static partial string m_ShowBlueprintPortraitPickerLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_NameOfTheNewCustomPortrait_LocalizedText", "Name of the new Custom Portrait:")]
    private static partial string m_NameOfTheNewCustomPortrait_LocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_UnknownID_LocalizedText", "Unknown Custom Portrait ID!")]
    private static partial string m_UnknownID_LocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_ChangePortraitLocalizedText", "Change Portrait")]
    private static partial string m_ChangePortraitLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_PortraitEditorFeature_m_NameOfTheNewBlueprintportrait_LocalizedText", "Name of the new Blueprintportrait:")]
    private static partial string m_NameOfTheNewBlueprintportrait_LocalizedText { get; }
}

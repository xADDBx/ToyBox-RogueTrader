using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
using ModKit;
using ModKit.DataViewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyBox.classes.Infrastructure.Blueprints;
using UnityEngine;

namespace ToyBox.PatchTool;
public class BlueprintPickerGUI {
    private string pickerText = "";
    private bool noSuchBP = false;
    private bool showBrowserPicker = false;
    private bool showBrowser = false;
    private Browser<SimpleBlueprint, SimpleBlueprint> browser = new(true) { DisplayShowAllGUI = false };
    private Type category = null;
    private string categorySearchString = "";
    private List<Type> categories = BlueprintIdCache.CachedIdTypes.Concat(typeof(SimpleBlueprint)).OrderBy(a => a.Name).ToList();
    private IEnumerable<SimpleBlueprint> bps;
    public void OnGUI(Action<string> callback, Type setCategory = null) {
        using (HorizontalScope()) {
            Space(20);
            using (VerticalScope()) {
                if (setCategory == null) DisclosureToggle("Show Browser Picker".localize(), ref showBrowserPicker);
                else category = setCategory;
                if (showBrowserPicker || setCategory != null) {
                    if (setCategory == null) {
                        if (GridPicker("BP Category", ref category, categories, "Nothing", t => t.Name, ref categorySearchString)) {
                            browser.ReloadData();
                            bps = null;
                        }
                        Space(20);
                    }
                    if (category != null) {
                        DisclosureToggle("Show Browser (starts loading all blueprints of the selected type)".localize(), ref showBrowser);
                        if (showBrowser) {
                            if ((bps?.Count() ?? 0) > 0) {
                                browser.OnGUI(bps, () => bps, bp => bp, bp => BlueprintExtensions.GetSearchKey(bp), bp => [BlueprintExtensions.GetSortKey(bp)], null,
                                    (bp, maybeBp) => {
                                        BlueprintExtensions.GetTitle(bp);
                                        string description = "";
                                        Func<string, string> titleFormatter = (t) => RichText.Bold(RichText.Orange(t));
                                        if (bp is BlueprintItem itemBlueprint && itemBlueprint.FlavorText?.Length > 0)
                                            description = $"{itemBlueprint.FlavorText.StripHTML().Color(RGBA.notable)}\n{description}";
                                        else description = bp.GetDescription() ?? "";
                                        using (HorizontalScope()) {
                                            string title = BlueprintExtensions.GetTitle(bp, name => RichText.Bold(RichText.Cyan(name)));
                                            Space(10);
                                            var typeString = bp.GetType().Name;
                                            using (HorizontalScope()) {
                                                ActionButton("Pick Blueprint".localize(), () => {
                                                    callback(bp.AssetGuid);
                                                });
                                                Space(17);
                                                Label(title, Width(300));
                                                ReflectionTreeView.DetailToggle("", bp, bp, 0);
                                                Space(-17);
                                                Label(typeString, rarityButtonStyle, AutoWidth());
                                                Space(17);
                                                ClipboardLabel(bp.AssetGuid.ToString(), ExpandWidth(false), Width(280));
                                                Space(17);
                                                if (description.Length > 0) Label(RichText.Green(description), Width(1000));
                                            }
                                        }
                                    });
                            } else if (Event.current.type == EventType.Repaint) {
                                bps = BlueprintLoader.BlueprintsOfType(category).NotNull();
                            }
                        }
                    }
                }
                Space(20);
                using (HorizontalScope()) {
                    Label("Enter target blueprint id".localize(), Width(200));
                    var before = pickerText;
                    TextField(ref pickerText, null, Width(350));
                    if (before != pickerText) {
                        noSuchBP = false;
                    }
                    ActionButton("Pick Blueprint".localize(), () => {
                        if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.ContainsKey(pickerText)) {
                            callback(pickerText);
                        } else {
                            noSuchBP = true;
                        }
                    });
                    if (noSuchBP) {
                        Space(20);
                        Label("No blueprint with that guid found.".localize().Yellow(), Width(300));
                    }
                }
            }
        }
    }
}
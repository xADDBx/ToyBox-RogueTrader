namespace ToyBox.Features.SettingsTab.Other;

public partial class GlyphSettingsFeature : Feature {
    [LocalizedString("ToyBox_Features_SettingsTab_Other_GlyphSettingsFeature_Name", "Glyphs")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_GlyphSettingsFeature_Description", "ToyBox uses some non-ascii characters (▼ ▶ ✎ ✔ ✖) in its UI. If the game font can't render them, ToyBox falls back to plain ASCII.")]
    public override partial string Description { get; }

    [LocalizedString("ToyBox_Features_SettingsTab_Other_GlyphSettingsFeature_CheckLabel", "Auto-detect glyph support")]
    private static partial string m_CheckLabel { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_GlyphSettingsFeature_CheckDescription", "Probe the game font on the first UI frame and pick glyphs or ASCII automatically.")]
    private static partial string m_CheckDescription { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_GlyphSettingsFeature_UseDefaultLabel", "Use fancy glyphs")]
    private static partial string m_UseDefaultLabel { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Other_GlyphSettingsFeature_UseDefaultDescription", "Off falls back to plain ASCII. Only used when auto-detect is disabled.")]
    private static partial string m_UseDefaultDescription { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            using (HorizontalScope()) {
                Space(27);
                UI.Label(Name);
                Space(10);
                UI.Label(Description.Green());
            }
            using (HorizontalScope()) {
                Space(27);
                _ = UI.Toggle(m_CheckLabel, m_CheckDescription, ref Settings.CheckForGlyphSupport, Glyphs.CheckGlyphSupport);
            }
            if (!Settings.CheckForGlyphSupport) {
                using (HorizontalScope()) {
                    Space(27);
                    _ = UI.Toggle(m_UseDefaultLabel, m_UseDefaultDescription, ref Settings.UseDefaultGlyphs);
                }
            }
        }
    }
}

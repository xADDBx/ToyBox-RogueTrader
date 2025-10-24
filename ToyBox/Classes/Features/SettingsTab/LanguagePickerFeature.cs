using System.Globalization;
using ToyBox.Infrastructure.Localization;

namespace ToyBox.Features.SettingsFeatures;
public partial class LanguagePickerFeature : Feature {
    [LocalizedString("ToyBox_Features_SettingsFeatures_LanguagePickerFeature_LanguagePickerText", "Language Picker")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_LanguagePickerFeature_PickYourCurrentUiLocaleText", "Pick your current ui locale")]
    public override partial string Description { get; }
    private static CultureInfo? m_UICulture;
    private readonly static Browser<CultureInfo> m_Browser = new((CultureInfo ci) => $"{ci.Name} {ci.NativeName} {ci.DisplayName} {ci.EnglishName}", (CultureInfo ci) => $"{ci.Name} {ci.NativeName} {ci.DisplayName} {ci.EnglishName}", [.. LocalizationManager.DiscoverLocalizations().Select(name => CultureInfo.GetCultureInfo(name))]);
    [LocalizedString("ToyBox_Features_SettingsFeatures_LanguagePickerFeature_CurrentCultureText", "Current Language")]
    private static partial string m_CurrentText { get; }
    public override void OnGui() {
        using (VerticalScope()) {
            using (HorizontalScope()) {
                UI.Label((m_CurrentText.Cyan() + ":").Bold());
                Space(25);
                m_UICulture ??= CultureInfo.GetCultureInfo(Settings.UILanguage);
                UI.Label($"{m_UICulture.NativeName} ({m_UICulture.Name})".Orange());
                Space(25);
            }
            Space(15);
            m_Browser.OnGUI((CultureInfo ci) => {
                using (HorizontalScope()) {
                    if (m_UICulture != ci) {
                        UI.Label($"{ci.NativeName} ({ci.Name})".Cyan(), Width(150));
                        Space(25);
                        if (UI.Button("Select".Cyan())) {
                            Settings.UILanguage = ci.Name;
                            m_UICulture = ci;
                            LocalizationManager.Update();
                        }
                    } else {
                        UI.Label($"{ci.NativeName} ({ci.Name})".Green(), Width(150));
                    }
                }
            });
        }
    }
}

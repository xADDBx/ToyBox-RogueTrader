namespace ToyBox.Infrastructure;
public static partial class UI {
    static UI() {
        Main.OnLocaleChanged += ClearLocaleCaches;
        Main.OnHideGUIAction += ClearHideCaches;
        Main.OnUIScaleChanged += UIScaleChanged;
    }
    private static void ClearLocaleCaches() {
        m_EnumCache.Clear();
        m_IndexToEnumCache.Clear();
        m_EnumNameCache.Clear();
    }
    private static void ClearHideCaches() {
        m_EditStateCaches.Clear();
    }
    private static void UIScaleChanged() {
        m_DisclosureToggleStyle = null;
    }
}

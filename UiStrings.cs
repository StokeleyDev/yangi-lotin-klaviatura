namespace UzbekKeyboard;

public enum AppLanguage {
    Uzbek,
    Russian
}

public sealed record Strings(
    string WindowTitle,
    string StatusDisabled,
    string StatusEnabled,
    string StatusEnabledEn,
    string StatusNeedLayout,
    string StatusErrorPrefix,
    string StatusElevated,
    string StatusPausedFormat,
    string StatusExcluded,
    string HotkeyUnavailableFormat,
    string TrayOpen,
    string TrayEnabled,
    string TrayWorkOnEnglish,
    string TrayConvertSelection,
    string TrayPauseFormat,
    string TrayExit,
    string TrayTooltipActive,
    string TrayTooltipPaused,
    string TrayTooltipDisabled
);

public static class UiStrings {
    public static readonly Strings Uzbek = new(
        WindowTitle: "Yangi lotin klaviatura",
        StatusDisabled: "O‘chirilgan",
        StatusEnabled: "Yoqilgan · o‘zbek lotin",
        StatusEnabledEn: "Yoqilgan · EN + o‘zbek lotin",
        StatusNeedLayout: "Win + Space: O‘zbek (lotin) tilini tanlang",
        StatusErrorPrefix: "Yoqib bo‘lmadi: ",
        StatusElevated: "{0} administrator huquqi bilan ishlayapti — bu oynada almashtirish ishlamaydi",
        StatusPausedFormat: "Pauza — davom ettirish: {0}",
        StatusExcluded: "{0} istisnolar ro‘yxatida — almashtirish o‘chirilgan",
        HotkeyUnavailableFormat: "{0} boshqa dastur tomonidan band",
        TrayOpen: "Ochish",
        TrayEnabled: "Avtomatik almashtirish",
        TrayWorkOnEnglish: "EN klaviaturada ishlash",
        TrayConvertSelection: "Tanlangan matnni o‘girish (Ctrl + Alt + L)",
        TrayPauseFormat: "Pauza ({0})",
        TrayExit: "Chiqish",
        TrayTooltipActive: "Yangi lotin · Yoqilgan",
        TrayTooltipPaused: "Yangi lotin · Pauza",
        TrayTooltipDisabled: "Yangi lotin · O‘chirilgan"
    );

    public static readonly Strings Russian = new(
        WindowTitle: "Yangi lotin klaviatura",
        StatusDisabled: "Отключено",
        StatusEnabled: "Включено · узбекская латиница",
        StatusEnabledEn: "Включено · EN + узбекская латиница",
        StatusNeedLayout: "Win + Пробел: выберите узбекский (латиница)",
        StatusErrorPrefix: "Не удалось включить: ",
        StatusElevated: "{0} запущено от имени администратора — в этом окне замена не работает",
        StatusPausedFormat: "Пауза — продолжить: {0}",
        StatusExcluded: "{0} в стоп-листе — замена отключена",
        HotkeyUnavailableFormat: "{0} занята другой программой",
        TrayOpen: "Открыть",
        TrayEnabled: "Автоматическая замена",
        TrayWorkOnEnglish: "Работать на EN-раскладке",
        TrayConvertSelection: "Конвертировать выделенное (Ctrl + Alt + L)",
        TrayPauseFormat: "Пауза ({0})",
        TrayExit: "Выход",
        TrayTooltipActive: "Yangi lotin · Включено",
        TrayTooltipPaused: "Yangi lotin · Пауза",
        TrayTooltipDisabled: "Yangi lotin · Отключено"
    );

    public static Strings Get(AppLanguage lang) => lang switch {
        AppLanguage.Russian => Russian,
        _ => Uzbek
    };
}

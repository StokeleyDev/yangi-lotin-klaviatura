namespace UzbekKeyboard;

public enum AppLanguage {
    Uzbek,
    Russian
}

public sealed record Strings(
    string WindowTitle,
    string HeaderTitle,
    string HeaderSubtitle,
    string LayoutBadge,
    string StatusCardTitle,
    string StatusDisabled,
    string StatusEnabled,
    string StatusErrorPrefix,
    string SettingsErrorPrefix,
    string ToggleActionEnable,
    string ToggleActionDisable,
    string ToggleAutomationName,
    string KeysCardTitle,
    string KeysCardSubtitle,
    string KeyHintGrave,
    string KeyHintBracketLeft,
    string KeyHintBracketRight,
    string KeyHintBackslash,
    string InputCardTitle,
    string InputPlaceholder,
    string InputAutomationName,
    string HintText,
    string SettingsButtonText,
    string SettingsAutomationName,
    string CloseAutomationName,
    string MinimizeAutomationName,
    string MaximizeAutomationName,
    string LangSwitcherAutomationName,
    string LangUzAutomationName,
    string LangRuAutomationName
);

public static class UiStrings {
    public static readonly Strings Uzbek = new(
        WindowTitle: "Yangi lotin klaviatura",
        HeaderTitle: "Yangi lotin klaviatura",
        HeaderSubtitle: "O‘zbekcha klaviatura",
        LayoutBadge: "UZB LAT",
        StatusCardTitle: "Klaviatura holati",
        StatusDisabled: "O‘chirilgan",
        StatusEnabled: "Yoqilgan · o‘zbek lotin",
        StatusErrorPrefix: "Yoqib bo‘lmadi: ",
        SettingsErrorPrefix: "Sozlamalarni ochib bo‘lmadi: ",
        ToggleActionEnable: "Yoqish",
        ToggleActionDisable: "O‘chirish",
        ToggleAutomationName: "Klaviaturani yoqish yoki o‘chirish",
        KeysCardTitle: "Yangi harflar",
        KeysCardSubtitle: "Standart klaviatura tugmalari yangi harflarga mos keladi",
        KeyHintGrave: "`",
        KeyHintBracketLeft: "[",
        KeyHintBracketRight: "]",
        KeyHintBackslash: "\\",
        InputCardTitle: "Sinab ko‘ring",
        InputPlaceholder: "Matnni bu yerga yozing...",
        InputAutomationName: "Klaviaturani tekshirish maydoni",
        HintText: "Windows sozlamalarida o‘zbek lotin tilini tanlang va klaviaturani yoqing. Oyna yopilganda amal to‘xtaydi.",
        SettingsButtonText: "Til sozlamalari",
        SettingsAutomationName: "Til sozlamalarini ochish",
        CloseAutomationName: "Yopish",
        MinimizeAutomationName: "Kichraytirish",
        MaximizeAutomationName: "Kattalashtirish",
        LangSwitcherAutomationName: "Tilni tanlash (UZ/RU)",
        LangUzAutomationName: "O‘zbek tili",
        LangRuAutomationName: "Rus tili"
    );

    public static readonly Strings Russian = new(
        WindowTitle: "Yangi lotin klaviatura",
        HeaderTitle: "Yangi lotin klaviatura",
        HeaderSubtitle: "Узбекская клавиатура",
        LayoutBadge: "UZB LAT",
        StatusCardTitle: "Раскладка",
        StatusDisabled: "Отключено",
        StatusEnabled: "Включено · узбекская латиница",
        StatusErrorPrefix: "Не удалось включить: ",
        SettingsErrorPrefix: "Не удалось открыть настройки: ",
        ToggleActionEnable: "Включить",
        ToggleActionDisable: "Выключить",
        ToggleAutomationName: "Переключить раскладку",
        KeysCardTitle: "Новые буквы",
        KeysCardSubtitle: "Клавиши стандартной клавиатуры соответствуют новым буквам",
        KeyHintGrave: "`",
        KeyHintBracketLeft: "[",
        KeyHintBracketRight: "]",
        KeyHintBackslash: "\\",
        InputCardTitle: "Попробуйте",
        InputPlaceholder: "Наберите текст здесь...",
        InputAutomationName: "Поле для проверки раскладки",
        HintText: "Выберите узбекский латинский язык в Windows и включите раскладку. При закрытии окна действие прекращается.",
        SettingsButtonText: "Настройки языка",
        SettingsAutomationName: "Открыть настройки языка",
        CloseAutomationName: "Закрыть",
        MinimizeAutomationName: "Свернуть",
        MaximizeAutomationName: "Развернуть",
        LangSwitcherAutomationName: "Выбор языка интерфейса (UZ/RU)",
        LangUzAutomationName: "Узбекский язык",
        LangRuAutomationName: "Русский язык"
    );

    public static Strings Get(AppLanguage lang) => lang switch {
        AppLanguage.Russian => Russian,
        _ => Uzbek
    };
}


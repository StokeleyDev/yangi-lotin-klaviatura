# Yangi lotin klaviatura — yangi o‘zbek alifbosi uchun Windows dasturi

**Yangi lotin klaviatura** — yangi o‘zbek alifbosida yozish uchun Windows klaviaturasi va matn konvertori. Dastur **sh → ş, ch → ç, o‘ → ö, g‘ → ğ** birikmalarini yozish paytida avtomatik almashtiradi va tayyor matnni yangi lotin harflariga o‘zgartiradi.

**Янги ўзбек алифбоси учун клавиатура:** Windows дастури янги лотин ҳарфларида ёзиш ва тайёр матнни ўзгартириш учун мўлжалланган.

**Новый узбекский алфавит на Windows:** клавиатура и преобразование текста с поддержкой букв **ş, ç, ö, ğ**. Интерфейс на узбекском и русском языках.

Joriy versiya / Текущая версия: **1.2.4**.

## Yuklab olish / Скачать

- [Windows uchun o‘rnatuvchi / Установщик](dist/Yangi%20lotin%20klaviatura%20Setup.exe?raw=true)
- [Portable ZIP — o‘rnatmasdan ishga tushirish / Без установки](dist/Yangi-Lotin-Portable.zip?raw=true)
- [Barcha versiyalar / Все версии](../../releases)

![Yangi o‘zbek alifbosi uchun Windows klaviaturasi — Yangi lotin klaviatura dasturi](assets/cover.png)

## Yangi o‘zbek alifbosida qanday yoziladi?

Windows tizimida **O‘zbek (lotin)** kiritish tilini tanlang va dasturdagi avtomatik almashtirishni yoqing. Odatiy harf birikmalarini tering; dastur ularni quyidagicha o‘zgartiradi:

| Birikma | Natija | Misol |
| --- | --- | --- |
| sh | ş | shahar → şahar |
| ch | ç | chiroyli → çiroyli |
| o‘ yoki o' | ö | o‘zbek → özbek |
| g‘ yoki g' | ğ | g‘oya → ğoya |

Tayyor matn uchun dasturdagi matn maydoniga yozing yoki matnni joylang va **O‘zgartirish** tugmasini bosing. Dastur kirill yozuvini to‘liq lotinga o‘girish uchun emas, yuqoridagi lotin harf birikmalarini almashtirish uchun mo‘ljallangan.

### Yangi lotin klaviatura qaysi Windows tizimida ishlaydi?

Windows x64, **.NET Desktop Runtime 8** va **Microsoft Edge WebView2 Runtime** talab qilinadi. O‘rnatish uchun yuqoridagi o‘rnatuvchini yuklab oling yoki portable ZIP arxivini to‘liq oching.

## Новое в 1.2.4

- Компактное окно 960×680 и размещение без прокрутки в обычном размере.
- Шрифт Inter, увеличенные подписи и полностью локальные файлы шрифта.
- Обновлённый светлый фон, официальные формулировки UZ/RU и значок приложения.
- Стекло на элементах управления, однотонные панели содержимого.

## Русский

Автоматическая замена **sh → ş, ch → ç, o‘ → ö, g‘ → ğ** при активной узбекской латинской раскладке Windows и преобразование готового текста. Интерфейс на узбекском и русском. В версии 1.2.4 большие панели и карточки однотонные; Liquid Glass сохранён на кнопках, переключателях и окне настроек. Светлая и тёмная темы используют локальные фоны с узбекскими мотивами.

Требуются **Windows x64**, **.NET Desktop Runtime 8** и **Microsoft Edge WebView2 Evergreen Runtime**. Установщик устанавливает программу для текущего пользователя, создаёт ярлык и предлагает автозапуск. Закройте программу перед обновлением. Для удаления используйте список установленных приложений Windows или Uninstall.exe в папке установки.

Portable ZIP нужно распаковать целиком: папки WebUI и runtimes должны оставаться рядом с EXE. Интернет для интерфейса не требуется. При закрытии окна автоматическая замена прекращается.

## O‘zbekcha

**sh → ş, ch → ç, o‘ → ö, g‘ → ğ** birikmalarini avtomatik almashtiring yoki tayyor matnni o‘zgartiring. Avtomatik almashtirish uchun Windows tizimida o‘zbek lotin kiritish tilini tanlang. Interfeys o‘zbek va rus tillarida. Yorug‘ va qorong‘i mavzular mavjud.

Windows x64, .NET Desktop Runtime 8 va Microsoft Edge WebView2 Runtime kerak. Yangilashdan oldin dasturni yoping. Portable ZIP arxivini to‘liq oching; WebUI va runtimes papkalarini EXE yonida saqlang. Oyna yopilganda almashtirish to‘xtaydi.

## English

Automatic replacement while typing with the Uzbek Latin Windows input language, plus native conversion of existing text. Uzbek/Russian interface. React and the actual @zakisheriff/liquid-glass library are hosted locally in WebView2; the keyboard engine remains C#.

Requires Windows x64, .NET 8 Desktop Runtime and Microsoft Edge WebView2 Evergreen Runtime. The installer supports a per-user installation, desktop shortcut, optional startup and uninstall registration. Close the app before updating. Extract the entire portable ZIP before running it.

## Screenshots

[Light theme](assets/interface-light.png) · [Dark theme](assets/interface-dark.png)

## Build

Install Node.js 18+ and .NET SDK 8 with Windows Desktop support. Close dev/preview servers, then run:

```powershell
./build-release.ps1
```

This builds the React UI, native host, uninstaller, embedded installer payload and both distributions under dist/. See [build details and verification limits](GLASS-BUILD.md).

Runtime downloads: [.NET Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) · [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/).

Release builds and headless UI checks pass. The production page and bridge were also checked in an invisible real WebView2 host. Desktop installation and global input in other applications have not been fully tested. Review converted text, especially borrowed words.

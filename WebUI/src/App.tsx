import { useEffect, useRef, useState } from 'react';
import { GlassButton, GlassSegmented, GlassSheet, GlassSwitch } from '@zakisheriff/liquid-glass';
import {
  ArrowLeftRight, ArrowRight, ArrowUpRight, Check, CheckCheck, Copy,
  Keyboard, Languages, Monitor, Moon, Plus, Settings2, Sun, X
} from 'lucide-react';
import appIcon from './assets/app-icon.png';

type Language = 'uz' | 'ru';
type ScriptKind = 'old' | 'new' | 'cyr';
type ConvertMode = 'oldToNew' | 'newToOld' | 'cyrToNew' | 'cyrToOld' | 'newToCyr' | 'oldToCyr';
type SelectionMode = 'auto' | ConvertMode;

type NativeState = {
  type: 'state';
  enabled: boolean;
  paused?: boolean;
  workOnEnglish?: boolean;
  altGrShortcuts?: boolean;
  smartTutuq?: boolean;
  theme?: number;
  pauseHotkey?: string;
  selectionHotkeyEnabled?: boolean;
  selectionConvertMode?: SelectionMode;
  selectionHotkeyRegistered?: boolean;
  excludedApps?: string[];
  hotkey?: string | null;
  updateVersion?: string | null;
  updateUrl?: string | null;
  language: Language;
  status: string;
  error?: string | null;
};

type WebView = {
  postMessage: (message: unknown) => void;
  addEventListener: (type: 'message', cb: (event: MessageEvent) => void) => void;
  removeEventListener: (type: 'message', cb: (event: MessageEvent) => void) => void;
};

declare global { interface Window { chrome?: { webview?: WebView } } }
const host = window.chrome?.webview;

const pauseHotkeyOptions = ['Ctrl+Alt+U', 'Ctrl+Alt+P', 'Ctrl+Shift+U', 'Ctrl+Alt+Pause'] as const;

const copy = {
  uz: {
    automatic: 'Avtoalmashtirish',
    keys: 'Harflar',
    converter: 'Konvertor',
    placeholder: "Shahar, o'zbek, Шаҳар…",
    convert: 'O‘girish',
    copied: 'Nusxalandi',
    copy: 'Nusxalash',
    swap: 'Yo‘nalishni almashtirish',
    scriptOld: 'Lotin (1995)',
    scriptNew: 'Yangi lotin',
    scriptCyr: 'Kirill',
    settings: 'Sozlamalar',
    groupInput: 'Kiritish',
    groupShortcuts: 'Tezkor tugmalar',
    groupExcluded: 'Istisnolar',
    groupAppearance: 'Ko‘rinish va Windows',
    system: 'Tizim',
    light: 'Yorug‘',
    dark: 'Qorong‘i',
    windows: 'Windows til',
    addUzbekLayout: '+ O‘zbek (lotin)',
    footer: 'Trayda ishlaydi',
    undo: 'bekor qilish',
    altGrHint: "+ S C O G '",
    ready: 'Ulanmoqda…',
    preview: 'Faqat Windows ilovasida ishlaydi',
    language: 'Interfeys tili',
    done: 'Tayyor',
    count: 'belgi',
    converted: 'Tayyor',
    pause: 'Pauza',
    workOnEnglish: 'EN-klaviatura',
    workOnEnglishCap: '3-tilsiz',
    altGr: 'Right Alt',
    altGrCap: "S, C, O, G, '",
    smartTutuq: 'Aqlli tutuq (ʼ)',
    smartTutuqCap: "ma'no → maʼno",
    selectionConverter: 'Belgilangan matn',
    selectionCap: 'Ctrl + Alt + L',
    selectionModeLabel: 'Yo‘nalish',
    selectionAuto: 'Avto',
    selectionUnavailable: 'Ctrl + Alt + L band',
    excludedPlaceholder: 'keepass.exe',
    add: 'Qo‘shish',
    remove: 'O‘chirish',
    copyError: 'Ctrl + C dan foydalaning',
    connectionError: 'Aloqa uzildi'
  },
  ru: {
    automatic: 'Автозамена',
    keys: 'Буквы',
    converter: 'Конвертер',
    placeholder: "Shahar, o'zbek, Шаҳар…",
    convert: 'Преобразовать',
    copied: 'Скопировано',
    copy: 'Копировать',
    swap: 'Поменять направление',
    scriptOld: 'Латиница (1995)',
    scriptNew: 'Новая латиница',
    scriptCyr: 'Кириллица',
    settings: 'Настройки',
    groupInput: 'Ввод',
    groupShortcuts: 'Горячие клавиши',
    groupExcluded: 'Исключения',
    groupAppearance: 'Оформление и Windows',
    system: 'Система',
    light: 'Светлая',
    dark: 'Тёмная',
    windows: 'Язык Windows',
    addUzbekLayout: '+ Узб. (латиница)',
    footer: 'Работает в трее',
    undo: 'отмена',
    altGrHint: "+ S C O G '",
    ready: 'Подключение…',
    preview: 'Работает только в Windows-приложении',
    language: 'Язык интерфейса',
    done: 'Готово',
    count: 'символов',
    converted: 'Готово',
    pause: 'Пауза',
    workOnEnglish: 'EN-раскладка',
    workOnEnglishCap: 'без 3-й раскладки',
    altGr: 'Right Alt',
    altGrCap: "S, C, O, G, '",
    smartTutuq: 'Умный тутуқ (ʼ)',
    smartTutuqCap: "ma'no → maʼno",
    selectionConverter: 'Выделенный текст',
    selectionCap: 'Ctrl + Alt + L',
    selectionModeLabel: 'Режим',
    selectionAuto: 'Авто',
    selectionUnavailable: 'Ctrl + Alt + L занята',
    excludedPlaceholder: 'keepass.exe',
    add: 'Добавить',
    remove: 'Удалить',
    copyError: 'Используйте Ctrl + C',
    connectionError: 'Нет ответа от приложения'
  }
};

const letters = [
  { from: 'sh', to: 'ş', word: 'shahar', result: 'şahar' },
  { from: 'ch', to: 'ç', word: 'chiroyli', result: 'çiroyli' },
  { from: "o'", to: 'ö', word: "o'zbek", result: 'özbek' },
  { from: "g'", to: 'ğ', word: "g'oya", result: 'ğoya' }
];

const themeKey = 'yangi-lotin-theme';

function toConvertMode(src: ScriptKind, dst: ScriptKind): ConvertMode {
  if (src === 'old' && dst === 'new') return 'oldToNew';
  if (src === 'new' && dst === 'old') return 'newToOld';
  if (src === 'cyr' && dst === 'new') return 'cyrToNew';
  if (src === 'cyr' && dst === 'old') return 'cyrToOld';
  if (src === 'new' && dst === 'cyr') return 'newToCyr';
  return 'oldToCyr';
}

export default function App() {
  const [language, setLanguage] = useState<Language>('uz');
  const [native, setNative] = useState<NativeState | null>(null);
  const [text, setText] = useState('');
  const [sourceScript, setSourceScript] = useState<ScriptKind>('old');
  const [targetScript, setTargetScript] = useState<ScriptKind>('new');
  const [sheet, setSheet] = useState(false);
  const [theme, setTheme] = useState(() => {
    try { const saved = Number(localStorage.getItem(themeKey)); return saved === 1 || saved === 2 ? saved : 0; }
    catch { return 0; }
  });
  const [notice, setNotice] = useState('');
  const [layoutNotice, setLayoutNotice] = useState('');
  const [pending, setPending] = useState(false);
  const [appName, setAppName] = useState('');
  const request = useRef(0);
  const timeout = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const editor = useRef<HTMLTextAreaElement>(null);
  const t = copy[language];

  useEffect(() => {
    if (!host) return;
    const receive = (event: MessageEvent) => {
      const message = event.data;
      if (message?.type === 'state' && (message.language === 'uz' || message.language === 'ru')) {
        setNative(message);
        setLanguage(message.language);
        if (typeof message.theme === 'number' && message.theme >= 0 && message.theme <= 2) {
          setTheme(message.theme);
        }
      } else if (message?.type === 'converted' && message.id === request.current) {
        clearTimeout(timeout.current);
        setPending(false);
        if (message.error) setNotice(message.error);
        else if (typeof message.text === 'string') {
          setText(message.text);
          setNotice(copy[document.documentElement.lang === 'ru' ? 'ru' : 'uz'].converted);
          editor.current?.focus();
        }
      } else if (message?.type === 'layoutResult' && typeof message.message === 'string') {
        setLayoutNotice(message.message);
      }
    };
    host.addEventListener('message', receive);
    host.postMessage({ action: 'ready' });
    return () => { host.removeEventListener('message', receive); clearTimeout(timeout.current); };
  }, []);

  useEffect(() => { document.documentElement.lang = language; }, [language]);
  useEffect(() => {
    if (theme === 0) delete document.documentElement.dataset.theme;
    else document.documentElement.dataset.theme = theme === 1 ? 'light' : 'dark';
    try { localStorage.setItem(themeKey, String(theme)); } catch { /* storage unavailable */ }
  }, [theme]);

  const changeLanguage = (index: number) => {
    const next = index === 0 ? 'uz' : 'ru';
    if (host) host.postMessage({ action: 'setLanguage', language: next });
    else setLanguage(next);
    setNotice('');
    setLayoutNotice('');
  };

  const changeTheme = (val: number) => {
    setTheme(val);
    host?.postMessage({ action: 'setTheme', theme: val });
  };

  const changeSourceScript = (next: ScriptKind) => {
    if (next === targetScript) setTargetScript(sourceScript);
    setSourceScript(next);
    setNotice('');
  };

  const changeTargetScript = (next: ScriptKind) => {
    if (next === sourceScript) setSourceScript(targetScript);
    setTargetScript(next);
    setNotice('');
  };

  const swapScripts = () => {
    setSourceScript(targetScript);
    setTargetScript(sourceScript);
    setNotice('');
  };

  const convert = () => {
    if (!host || !native || pending) return;
    setPending(true);
    setNotice('');
    const id = ++request.current;
    const mode = toConvertMode(sourceScript, targetScript);
    host.postMessage({ action: 'convert', text, mode, id });
    timeout.current = setTimeout(() => { setPending(false); setNotice(t.connectionError); request.current++; }, 10000);
  };

  const excludedApps = native?.excludedApps ?? [];
  const setExcluded = (apps: string[]) => host?.postMessage({ action: 'setExcludedApps', apps });
  const addApp = () => { const name = appName.trim(); if (!name) return; setExcluded([...excludedApps, name]); setAppName(''); };
  const copyText = async () => {
    try { await navigator.clipboard.writeText(text); setNotice(t.copied); }
    catch { setNotice(t.copyError); editor.current?.focus(); editor.current?.select(); }
  };

  const scriptLabel = (s: ScriptKind) => s === 'old' ? t.scriptOld : s === 'new' ? t.scriptNew : t.scriptCyr;

  return (
    <div className="app">
      <div className="landscape" aria-hidden="true" />
      <div className="content">
        <header className="toolbar">
          <div className="brand">
            <img className="brand-icon" src={appIcon} width="24" height="24" alt="" />
            <span>Yangi lotin</span>
            {native?.updateVersion && native.updateUrl && (
              <button
                type="button"
                className="update-pill"
                onClick={() => host?.postMessage({ action: 'openUpdateUrl', url: native.updateUrl })}
              >
                <span>v{native.updateVersion}</span>
                <ArrowUpRight size={13} />
              </button>
            )}
          </div>
          <div className="toolbar-actions">
            <GlassSegmented options={['UZ', 'RU']} value={language === 'uz' ? 0 : 1} onChange={changeLanguage} aria-label={t.language} />
            <GlassButton icon={<Settings2 size={19} />} aria-label={t.settings} onClick={() => setSheet(true)} />
          </div>
        </header>
        {!host && <p className="preview-note"><Monitor size={14} />{t.preview}</p>}
        <main aria-label="Yangi lotin klaviatura">
          <section className="solid-panel status-card">
            <div className="status-icon"><Keyboard size={24} strokeWidth={1.5} /></div>
            <div className="status-content">
              <h2>{t.automatic}</h2>
              <div className={'status-line ' + (native?.error ? 'error' : '')} role="status">
                <span className={'status-dot ' + (native?.enabled && !native.paused ? 'on' : '')} />
                {native?.status ?? (host ? t.ready : 'Windows · UZB LAT')}
              </div>
            </div>
            <GlassSwitch
              checked={native?.enabled ?? false}
              onChange={enabled => host?.postMessage({ action: 'setEnabled', enabled })}
              disabled={!native}
              aria-label={t.automatic}
            />
          </section>

          <section className="letters-section">
            <div className="section-heading">
              <h2>{t.keys}</h2>
              <span className="key-label">{native?.workOnEnglish ? 'UZB + EN' : 'UZB LAT'}</span>
            </div>
            <div className="letter-grid">
              {letters.map(item => (
                <div key={item.to} className="solid-panel letter-card">
                  <div className="key-transform"><kbd>{item.from}</kbd><ArrowRight size={14} /></div>
                  <div className="new-letter">{item.to}</div>
                  <div className="word"><span>{item.word}</span><ArrowRight size={12} /><strong>{item.result}</strong></div>
                </div>
              ))}
            </div>
            <div className="undo-hint">
              <kbd>⌫</kbd><span>{t.undo}</span>
              {(native?.altGrShortcuts ?? true) && (
                <>
                  <span className="hint-sep">·</span>
                  <kbd>Right Alt</kbd><span>{t.altGrHint}</span>
                </>
              )}
            </div>
          </section>

          <section className="solid-panel converter">
            <div className="section-heading">
              <h2>{t.converter}</h2>
              <Languages size={20} strokeWidth={1.5} />
            </div>
            <div className="converter-direction">
              <select
                className="native-select"
                value={sourceScript}
                onChange={e => changeSourceScript(e.target.value as ScriptKind)}
                aria-label={t.converter + ' source'}
              >
                <option value="old">{scriptLabel('old')}</option>
                <option value="new">{scriptLabel('new')}</option>
                <option value="cyr">{scriptLabel('cyr')}</option>
              </select>
              <GlassButton icon={<ArrowLeftRight size={15} />} aria-label={t.swap} onClick={swapScripts} />
              <select
                className="native-select"
                value={targetScript}
                onChange={e => changeTargetScript(e.target.value as ScriptKind)}
                aria-label={t.converter + ' target'}
              >
                <option value="new">{scriptLabel('new')}</option>
                <option value="old">{scriptLabel('old')}</option>
                <option value="cyr">{scriptLabel('cyr')}</option>
              </select>
            </div>
            <textarea
              ref={editor}
              value={text}
              onChange={e => { setText(e.target.value); setNotice(''); }}
              readOnly={pending}
              maxLength={200000}
              placeholder={t.placeholder}
              aria-label={t.converter}
              spellCheck={false}
            />
            <div className="converter-footer">
              <div className="editor-meta">
                <span>{text.length.toLocaleString()} {t.count}</span>
                <span role="status" className="notice">{notice}</span>
              </div>
              <div className="editor-actions">
                <GlassButton
                  icon={notice === t.copied ? <Check size={16} /> : <Copy size={16} />}
                  aria-label={t.copy}
                  disabled={!text || pending}
                  onClick={copyText}
                />
                <GlassButton
                  variant="filled"
                  onClick={convert}
                  disabled={!native || !text || pending}
                  icon={<CheckCheck size={17} />}
                >
                  {t.convert}
                </GlassButton>
              </div>
            </div>
          </section>
        </main>
        <footer><span className="tiny-dot" />{t.footer}</footer>
      </div>

      <GlassSheet open={sheet} onClose={() => setSheet(false)} aria-label={t.settings}>
        <div className="settings-content">
          <h2>{t.settings}</h2>

          <div className="settings-group">
            <div className="settings-group-title">{t.groupInput}</div>
            <div className="settings-card">
              <div className="settings-item">
                <div className="settings-item-text">
                  <span className="settings-item-title">{t.workOnEnglish}</span>
                  <span className="settings-item-cap">{t.workOnEnglishCap}</span>
                </div>
                <GlassSwitch
                  checked={native?.workOnEnglish ?? false}
                  onChange={enabled => host?.postMessage({ action: 'setWorkOnEnglish', enabled })}
                  disabled={!native}
                  aria-label={t.workOnEnglish}
                />
              </div>
              <div className="settings-item">
                <div className="settings-item-text">
                  <span className="settings-item-title">{t.altGr}</span>
                  <span className="settings-item-cap">{t.altGrCap}</span>
                </div>
                <GlassSwitch
                  checked={native?.altGrShortcuts ?? true}
                  onChange={enabled => host?.postMessage({ action: 'setAltGrShortcuts', enabled })}
                  disabled={!native}
                  aria-label={t.altGr}
                />
              </div>
              <div className="settings-item">
                <div className="settings-item-text">
                  <span className="settings-item-title">{t.smartTutuq}</span>
                  <span className="settings-item-cap">{t.smartTutuqCap}</span>
                </div>
                <GlassSwitch
                  checked={native?.smartTutuq ?? false}
                  onChange={enabled => host?.postMessage({ action: 'setSmartTutuq', enabled })}
                  disabled={!native}
                  aria-label={t.smartTutuq}
                />
              </div>
            </div>
          </div>

          <div className="settings-group">
            <div className="settings-group-title">{t.groupShortcuts}</div>
            <div className="settings-card">
              <div className="settings-item">
                <div className="settings-item-text">
                  <span className="settings-item-title">{t.pause}</span>
                  {native?.hotkey && <span className="settings-item-cap error">{native.hotkey}</span>}
                </div>
                <div className="settings-item-controls">
                  <select
                    className="native-select compact-select"
                    value={native?.pauseHotkey ?? 'Ctrl+Alt+U'}
                    disabled={!native}
                    onChange={e => host?.postMessage({ action: 'setPauseHotkey', hotkey: e.target.value })}
                    aria-label={t.pause}
                  >
                    {pauseHotkeyOptions.map(opt => <option key={opt} value={opt}>{opt}</option>)}
                  </select>
                  <GlassSwitch
                    checked={native?.paused ?? false}
                    onChange={paused => host?.postMessage({ action: 'setPaused', paused })}
                    disabled={!native}
                    aria-label={t.pause}
                  />
                </div>
              </div>
              <div className="settings-item">
                <div className="settings-item-text">
                  <span className="settings-item-title">{t.selectionConverter}</span>
                  <span className={'settings-item-cap' + (native?.selectionHotkeyEnabled && native.selectionHotkeyRegistered === false ? ' error' : '')}>
                    {native?.selectionHotkeyEnabled && native.selectionHotkeyRegistered === false
                      ? t.selectionUnavailable
                      : t.selectionCap}
                  </span>
                </div>
                <GlassSwitch
                  checked={native?.selectionHotkeyEnabled ?? true}
                  onChange={enabled => host?.postMessage({ action: 'setSelectionHotkey', enabled })}
                  disabled={!native}
                  aria-label={t.selectionConverter}
                />
              </div>
              <div className="settings-item">
                <span className="settings-item-title">{t.selectionModeLabel}</span>
                <select
                  className="native-select compact-select"
                  value={native?.selectionConvertMode ?? 'auto'}
                  disabled={!native || !(native?.selectionHotkeyEnabled ?? true)}
                  onChange={e => host?.postMessage({ action: 'setSelectionConvertMode', mode: e.target.value })}
                  aria-label={t.selectionModeLabel}
                >
                  <option value="auto">{t.selectionAuto}</option>
                  <option value="oldToNew">{t.scriptOld} → {t.scriptNew}</option>
                  <option value="newToOld">{t.scriptNew} → {t.scriptOld}</option>
                  <option value="cyrToNew">{t.scriptCyr} → {t.scriptNew}</option>
                  <option value="cyrToOld">{t.scriptCyr} → {t.scriptOld}</option>
                  <option value="newToCyr">{t.scriptNew} → {t.scriptCyr}</option>
                  <option value="oldToCyr">{t.scriptOld} → {t.scriptCyr}</option>
                </select>
              </div>
            </div>
          </div>

          <div className="settings-group">
            <div className="settings-group-title">{t.groupExcluded}</div>
            <div className="settings-card settings-card--pad">
              {excludedApps.length > 0 && (
                <ul className="excluded-list" aria-label={t.groupExcluded}>
                  {excludedApps.map(app => (
                    <li key={app}>
                      <span>{app}</span>
                      <GlassButton icon={<X size={14} />} aria-label={t.remove + ' ' + app} disabled={!native} onClick={() => setExcluded(excludedApps.filter(x => x !== app))} />
                    </li>
                  ))}
                </ul>
              )}
              <form className="excluded-add" onSubmit={e => { e.preventDefault(); addApp(); }}>
                <input
                  value={appName}
                  onChange={e => setAppName(e.target.value)}
                  placeholder={t.excludedPlaceholder}
                  aria-label={t.excludedPlaceholder}
                  maxLength={200}
                  spellCheck={false}
                  disabled={!native}
                />
                <GlassButton variant="tinted" icon={<Plus size={16} />} disabled={!native || !appName.trim()} onClick={addApp}>{t.add}</GlassButton>
              </form>
            </div>
          </div>

          <div className="settings-group">
            <div className="settings-group-title">{t.groupAppearance}</div>
            <div className="settings-card settings-card--pad">
              <GlassSegmented
                options={[{ label: t.system, icon: <Monitor size={15} /> }, { label: t.light, icon: <Sun size={15} /> }, { label: t.dark, icon: <Moon size={15} /> }]}
                value={theme}
                onChange={changeTheme}
                aria-label={t.groupAppearance}
              />
              <div className="windows-actions">
                <GlassButton variant="tinted" disabled={!native} onClick={() => host?.postMessage({ action: 'addUzbekLayout' })} icon={<Plus size={15} />}>
                  {t.addUzbekLayout}
                </GlassButton>
                <GlassButton variant="tinted" disabled={!native} onClick={() => host?.postMessage({ action: 'settings' })} icon={<Settings2 size={15} />}>
                  {t.windows}
                </GlassButton>
              </div>
              {layoutNotice && <p className="settings-item-cap" role="status">{layoutNotice}</p>}
            </div>
          </div>

          <GlassButton variant="filled" onClick={() => setSheet(false)}>{t.done}</GlassButton>
        </div>
      </GlassSheet>
    </div>
  );
}

import { useEffect, useRef, useState } from 'react';
import { GlassButton, GlassSegmented, GlassSheet, GlassSwitch } from '@zakisheriff/liquid-glass';
import { ArrowRight, Check, CheckCheck, Copy, Keyboard, Languages, Monitor, Moon, Settings2, Sun } from 'lucide-react';
import appIcon from './assets/app-icon.png';

type Language = 'uz' | 'ru';
type NativeState = {type: 'state'; enabled: boolean; language: Language; status: string; error?: string | null};
type WebView = {postMessage: (message: unknown) => void; addEventListener: (type: 'message', cb: (event: MessageEvent) => void) => void; removeEventListener: (type: 'message', cb: (event: MessageEvent) => void) => void};
declare global { interface Window { chrome?: {webview?: WebView} } }
const host = window.chrome?.webview;
const copy = {
  uz: {
    automatic: 'Avtomatik almashtirish', automaticHint: 'O‘zbek lotin tili tanlanganda barcha ilovalarda ishlaydi',
    keys: 'Harf birikmalari va mos harflar', keysHint: 'Birikmalarni yangi o‘zbek alifbosidagi harflarga avtomatik almashtirish',
    converter: 'Tayyor matnni o‘zgartirish', converterHint: 'Matnni joylang va yangi lotinga o‘giring.',
    placeholder: "Shahar, chiroyli, o'zbek, g'oya…", convert: 'O‘zgartirish', copied: 'Nusxalandi', copy: 'Nusxalash',
    settings: 'Sozlamalar', appearance: 'Ko‘rinish', system: 'Tizim', light: 'Yorug‘', dark: 'Qorong‘i',
    windows: 'Windows til sozlamalari', windowsHint: 'O‘zbek (lotin) tilini qo‘shing va Win + Space orqali tanlang.',
    footer: 'Oyna yopilganda avtomatik almashtirish to‘xtaydi.', undo: 'Almashtirishni bekor qilish',
    ready: 'Ulanmoqda…', preview: 'Interfeys ko‘rinishi — klaviatura faqat Windows ilovasida ishlaydi',
    language: 'Interfeys tili', done: 'Tayyor', count: 'belgi', converted: 'Matn o‘zgartirildi', limit: '200 000 belgigacha',
    copyError: 'Nusxalab bo‘lmadi. Ctrl + C dan foydalaning.', connectionError: 'Ilova bilan aloqa uzildi. Qayta oching.'
  },
  ru: {
    automatic: 'Автоматическая замена', automaticHint: 'Работает во всех приложениях с узбекской латиницей',
    keys: 'Соответствие буквенных сочетаний', keysHint: 'Автоматическое преобразование сочетаний в буквы нового узбекского алфавита',
    converter: 'Преобразование текста', converterHint: 'Вставьте текст и переведите в новую латиницу.',
    placeholder: "Shahar, chiroyli, o'zbek, g'oya…", convert: 'Преобразовать', copied: 'Скопировано', copy: 'Копировать',
    settings: 'Настройки', appearance: 'Оформление', system: 'Системное', light: 'Светлое', dark: 'Тёмное',
    windows: 'Настройки языка Windows', windowsHint: 'Добавьте узбекский (латиница) и выберите его через Win + Пробел.',
    footer: 'При закрытии окна автоматическая замена прекращается.', undo: 'Отменить замену',
    ready: 'Подключение…', preview: 'Предпросмотр — клавиатура работает только в Windows-приложении',
    language: 'Язык интерфейса', done: 'Готово', count: 'символов', converted: 'Текст преобразован', limit: 'До 200 000 символов',
    copyError: 'Не удалось скопировать. Используйте Ctrl + C.', connectionError: 'Нет ответа от приложения. Откройте его заново.'
  }
};
const letters = [{from:'sh', to:'ş', word:'shahar', result:'şahar'}, {from:'ch', to:'ç', word:'chiroyli', result:'çiroyli'}, {from:"o'", to:'ö', word:"o'zbek", result:'özbek'}, {from:"g'", to:'ğ', word:"g'oya", result:'ğoya'}];

export default function App() {
  const [language, setLanguage] = useState<Language>('uz');
  const [native, setNative] = useState<NativeState | null>(null);
  const [text, setText] = useState('');
  const [sheet, setSheet] = useState(false);
  const [theme, setTheme] = useState(0);
  const [notice, setNotice] = useState('');
  const [pending, setPending] = useState(false);
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
      } else if (message?.type === 'converted' && message.id === request.current) {
        clearTimeout(timeout.current);
        setPending(false);
        if (message.error) setNotice(message.error);
        else if (typeof message.text === 'string') {
          setText(message.text);
          setNotice(copy[document.documentElement.lang === 'ru' ? 'ru' : 'uz'].converted);
          editor.current?.focus();
        }
      }
    };
    host.addEventListener('message', receive);
    host.postMessage({action: 'ready'});
    return () => { host.removeEventListener('message', receive); clearTimeout(timeout.current); };
  }, []);
  useEffect(() => { document.documentElement.lang = language; }, [language]);
  useEffect(() => {
    if (theme === 0) delete document.documentElement.dataset.theme;
    else document.documentElement.dataset.theme = theme === 1 ? 'light' : 'dark';
  }, [theme]);

  const changeLanguage = (index: number) => {
    const next = index === 0 ? 'uz' : 'ru';
    if (host) host.postMessage({action: 'setLanguage', language: next});
    else setLanguage(next);
    setNotice('');
  };
  const convert = () => {
    if (!host || !native || pending) return;
    setPending(true); setNotice('');
    const id = ++request.current;
    host.postMessage({action: 'convert', text, id});
    timeout.current = setTimeout(() => { setPending(false); setNotice(t.connectionError); request.current++; }, 10000);
  };
  const copyText = async () => {
    try { await navigator.clipboard.writeText(text); setNotice(t.copied); }
    catch { setNotice(t.copyError); editor.current?.focus(); editor.current?.select(); }
  };
  return (
    <div className="app">
      <div className="landscape" aria-hidden="true"/>
      <div className="content">
        <header className="toolbar">
          <div className="brand"><img className="brand-icon" src={appIcon} width="24" height="24" alt=""/><span>Yangi lotin</span></div>
          <div className="toolbar-actions">
            <GlassSegmented options={['UZ', 'RU']} value={language === 'uz' ? 0 : 1} onChange={changeLanguage} aria-label={t.language}/>
            <GlassButton icon={<Settings2 size={19}/>} aria-label={t.settings} onClick={() => setSheet(true)}/>
          </div>
        </header>
        {!host && <p className="preview-note"><Monitor size={14}/>{t.preview}</p>}
        <main aria-label="Yangi lotin klaviatura">
          <section className="solid-panel status-card">
            <div className="status-icon"><Keyboard size={25} strokeWidth={1.5}/></div>
            <div className="status-content"><h2>{t.automatic}</h2><p>{t.automaticHint}</p>
              <div className={'status-line ' + (native?.error ? 'error' : '')} role="status"><span className={'status-dot ' + (native?.enabled ? 'on' : '')}/>{native?.status ?? (host ? t.ready : 'Windows · UZB LAT')}</div>
            </div>
            <GlassSwitch checked={native?.enabled ?? false} onChange={enabled => host?.postMessage({action:'setEnabled', enabled})} disabled={!native} aria-label={t.automatic}/>
          </section>
          <section className="letters-section">
            <div className="section-heading"><div><h2>{t.keys}</h2><p>{t.keysHint}</p></div><span className="key-label">UZB LAT</span></div>
            <div className="letter-grid">
              {letters.map(item => <div key={item.to} className="solid-panel letter-card">
                <div className="key-transform"><kbd>{item.from}</kbd><ArrowRight size={14}/></div>
                <div className="new-letter">{item.to}</div>
                <div className="word"><span>{item.word}</span><ArrowRight size={12}/><strong>{item.result}</strong></div>
              </div>)}
            </div>
            <div className="undo-hint"><kbd>⌫</kbd><span>{t.undo}</span></div>
          </section>
          <section className="solid-panel converter">
            <div className="section-heading"><div><h2>{t.converter}</h2><p>{t.converterHint}</p></div><Languages size={22} strokeWidth={1.5}/></div>
            <textarea ref={editor} value={text} onChange={e => {setText(e.target.value); setNotice('');}} readOnly={pending} maxLength={200000} placeholder={t.placeholder} aria-label={t.converter} spellCheck={false}/>
            <div className="converter-footer"><div className="editor-meta"><span>{text.length.toLocaleString()} {t.count}</span><span role="status" className="notice">{notice}</span></div>
              <div className="editor-actions">
                <GlassButton icon={notice === t.copied ? <Check size={16}/> : <Copy size={16}/>} aria-label={t.copy} disabled={!text || pending} onClick={copyText}/>
                <GlassButton variant="filled" onClick={convert} disabled={!native || !text || pending} icon={<CheckCheck size={17}/>}>{t.convert}</GlassButton>
              </div>
            </div>
          </section>
        </main>
        <footer><span className="tiny-dot"/>{t.footer}</footer>
      </div>
      <GlassSheet open={sheet} onClose={() => setSheet(false)} aria-label={t.settings}>
        <div className="settings-content">
          <h2>{t.settings}</h2>
          <div className="setting-row"><Languages size={21}/><h3>{t.language}</h3></div>
          <GlassSegmented options={['O‘zbek', 'Русский']} value={language === 'uz' ? 0 : 1} onChange={changeLanguage} aria-label={t.language}/>
          <div className="setting-row"><Sun size={21}/><h3>{t.appearance}</h3></div>
          <GlassSegmented options={[{label:t.system, icon:<Monitor size={16}/>},{label:t.light,icon:<Sun size={16}/>},{label:t.dark,icon:<Moon size={16}/>}]} value={theme} onChange={setTheme} aria-label={t.appearance}/>
          <div className="windows-setting"><p>{t.windowsHint}</p><GlassButton variant="tinted" disabled={!native} onClick={() => host?.postMessage({action:'settings'})} icon={<Settings2 size={16}/>}>{t.windows}</GlassButton></div>
          <GlassButton variant="filled" onClick={() => setSheet(false)}>{t.done}</GlassButton>
        </div>
      </GlassSheet>
    </div>
  );
}

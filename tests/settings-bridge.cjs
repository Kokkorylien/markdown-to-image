const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

async function main() {
  const root = path.join(__dirname, '..');
  const catalogs = JSON.parse(fs.readFileSync(path.join(root, 'src', 'locales.json'), 'utf8'));
  assert.deepEqual(Object.keys(catalogs.en).sort(), Object.keys(catalogs['zh-CN']).sort());
  const html = fs.readFileSync(path.join(root, 'src', 'settings.html'), 'utf8');
  const native = fs.readFileSync(path.join(root, 'native', 'App.cs'), 'utf8');
  const keys = [...html.matchAll(/data-i18n="([^"]+)"/g), ...native.matchAll(/L\.T\("([^"]+)"\)/g)].map(value => value[1]);
  for (const language of ['zh-CN', 'en']) for (const key of keys) assert.equal(typeof catalogs[language][key], 'string', `Missing ${language} translation: ${key}`);
  const elements = new Map();
  const element = id => {
    if (!elements.has(id)) elements.set(id, {
      value: '', textContent: '', checked: false, disabled: false, handlers: {}, dataset: {},
      addEventListener(type, handler) { this.handlers[type] = handler; }
    });
    return elements.get(id);
  };
  let messageListener;
  element('export').dataset.i18n = 'saveClipboard';
  const state = { hotkey: 'Control+Alt+Shift+M', pasteHotkey: 'Control+Alt+V', outputDir: 'C:\\Pictures\\Markdown', width: 1080, theme: 'dark', language: 'zh-CN', copyAfterExport: true, status: '准备就绪', exporting: false };
  const requests = [];
  const context = vm.createContext({
    document: { getElementById: element, querySelectorAll: () => [...elements.values()].filter(value => value.dataset.i18n), documentElement: { dataset: {} } },
    markdownTranslations: catalogs,
    chrome: { webview: {
      addEventListener(type, callback) { assert.equal(type, 'message'); messageListener = callback; },
      postMessage(message) {
        requests.push(message);
        queueMicrotask(() => {
          if (message.channel === 'set-copy-after-export') state.copyAfterExport = message.value;
          if (message.channel === 'set-theme') state.theme = message.value;
          if (message.channel === 'set-language') { state.language = message.value; state.status = catalogs[message.value].languageSaved; }
          if (message.channel === 'save-config') Object.assign(state, message.value);
          // Status pushes can arrive while a settings request is still pending.
          messageListener({ data: { statusEvent: true, value: { ...state } } });
          messageListener({ data: { id: message.id, ok: true, value: { ...state } } });
        });
      }
    } }
  });
  context.window = context;
  vm.runInContext(fs.readFileSync(path.join(root, 'native', 'bridge.js'), 'utf8'), context, { filename: 'bridge.js' });
  vm.runInContext(fs.readFileSync(path.join(root, 'src', 'i18n.js'), 'utf8'), context, { filename: 'i18n.js' });
  vm.runInContext(fs.readFileSync(path.join(root, 'src', 'settings.js'), 'utf8'), context, { filename: 'settings.js' });
  await new Promise(setImmediate);
  assert.equal(element('status').textContent, '准备就绪', 'The UI and native bridge interfere when loaded in the same page');
  assert.equal(element('paste-hotkey').value, state.pasteHotkey);
  assert.equal(element('theme').value, 'dark');
  assert.equal(context.document.documentElement.dataset.theme, 'dark');
  const language = element('language');
  language.value = 'en';
  await language.handlers.change({ target: language });
  assert.equal(state.language, 'en');
  assert.equal(context.document.documentElement.lang, 'en');
  assert.equal(element('export').textContent, catalogs.en.saveClipboard);
  assert.equal(element('status').textContent, catalogs.en.languageSaved);
  language.value = 'zh-CN';
  await language.handlers.change({ target: language });
  assert.equal(element('export').textContent, catalogs['zh-CN'].saveClipboard);
  assert.equal(context.document.documentElement.lang, 'zh-CN');
  assert.equal(element('copy-after-export').checked, true);
  const toggle = element('copy-after-export');
  toggle.checked = false;
  await toggle.handlers.change({ target: toggle });
  assert.equal(toggle.checked, false);
  assert.equal(toggle.disabled, false);
  const theme = element('theme');
  theme.value = 'light';
  await theme.handlers.change({ target: theme });
  assert.equal(state.theme, 'light');
  assert.equal(context.document.documentElement.dataset.theme, 'light');
  assert.equal(theme.disabled, false);
  theme.value = 'dark';
  await theme.handlers.change({ target: theme });
  assert.equal(state.theme, 'dark');
  assert.equal(context.document.documentElement.dataset.theme, 'dark');
  element('width').value = '1440';
  await element('settings').handlers.submit({ preventDefault() {} });
  assert.equal(state.width, 1440);
  assert.equal(element('saved').textContent, '已保存');
  assert.deepEqual(requests.map(value => value.channel), ['get-state', 'settings-ready', 'set-language', 'set-language', 'set-copy-after-export', 'set-theme', 'set-theme', 'save-config']);
  console.log('PASS: shared-page native bridge, initial settings, status pushes, toggle, settings save');
}
main().catch(error => { console.error(error); process.exitCode = 1; });

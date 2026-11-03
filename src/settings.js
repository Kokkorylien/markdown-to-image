const tool = window.markdownTool;
const byId = id => document.getElementById(id);
const prettyHotkey = value => value.replace(/Control/g, 'Ctrl').replace(/CommandOrControl/g, 'Ctrl');
let appliedTheme = 'light';
let appliedLanguage;

function applyLanguage(language) {
  appliedLanguage = language;
  window.markdownI18n.apply(language);
  byId('language').value = language;
  if (byId('saved').textContent) byId('saved').textContent = window.markdownI18n.text('saved');
}

function applyTheme(theme) {
  appliedTheme = theme;
  document.documentElement.dataset.theme = theme;
  byId('theme').value = theme;
}

function updateStatus(state) {
  if (state.language !== appliedLanguage) applyLanguage(state.language);
  byId('status').textContent = state.status;
  byId('export').disabled = state.exporting;
  byId('copy-image').disabled = state.exporting;
  byId('open-recent').disabled = !state.recentFile;
}
function populate(state) {
  byId('output-dir').textContent = state.outputDir;
  byId('hotkey').value = state.hotkey;
  byId('hotkey-label').textContent = prettyHotkey(state.hotkey);
  byId('paste-hotkey').value = state.pasteHotkey;
  byId('paste-hotkey-label').textContent = prettyHotkey(state.pasteHotkey);
  byId('width').value = state.width;
  applyTheme(state.theme);
  byId('copy-after-export').checked = state.copyAfterExport;
  updateStatus(state);
}
async function invoke(action) {
  try {
    const result = await action();
    if (!result.ok) throw new Error(result.message);
    return result.value;
  } catch (error) {
    byId('status').textContent = error.message;
  }
}
byId('export').addEventListener('click', () => invoke(tool.exportClipboard));
byId('copy-image').addEventListener('click', () => invoke(tool.copyClipboardImage));
byId('language').addEventListener('change', async event => {
  const select = event.target;
  const previous = appliedLanguage;
  const language = select.value;
  applyLanguage(language);
  select.disabled = true;
  try {
    const value = await invoke(() => tool.setLanguage(language));
    applyLanguage(value ? value.language : previous);
  } finally { select.disabled = false; }
});
byId('theme').addEventListener('change', async event => {
  const select = event.target;
  const previous = appliedTheme;
  const theme = select.value;
  applyTheme(theme);
  select.disabled = true;
  try {
    const value = await invoke(() => tool.setTheme(theme));
    applyTheme(value ? value.theme : previous);
  } finally { select.disabled = false; }
});
byId('copy-after-export').addEventListener('change', async event => {
  const toggle = event.target;
  const enabled = toggle.checked;
  toggle.disabled = true;
  try {
    const value = await invoke(() => tool.setCopyAfterExport(enabled));
    toggle.checked = value ? value.copyAfterExport : !enabled;
  } finally { toggle.disabled = false; }
});
byId('open-folder').addEventListener('click', () => invoke(tool.openFolder));
byId('open-recent').addEventListener('click', () => invoke(tool.openRecent));
byId('choose-folder').addEventListener('click', async () => {
  const value = await invoke(tool.chooseFolder);
  if (value) byId('output-dir').textContent = value.outputDir;
});
byId('settings').addEventListener('submit', async event => {
  event.preventDefault();
  const value = await invoke(() => tool.saveConfig({ hotkey: byId('hotkey').value, pasteHotkey: byId('paste-hotkey').value, width: Number(byId('width').value), theme: byId('theme').value }));
  if (value) { populate(value); byId('saved').textContent = window.markdownI18n.text('saved'); }
});
tool.onStatus(updateStatus);
invoke(tool.getState).then(value => { if (value) { populate(value); tool.ready(); } });

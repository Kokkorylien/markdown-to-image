(() => {
const waiting = new Map();
let nextId = 0;
function invoke(channel, value) {
  const id = ++nextId;
  return new Promise(resolve => {
    waiting.set(id, resolve);
    chrome.webview.postMessage({ id, channel, value });
  });
}
const listeners = [];
chrome.webview.addEventListener('message', event => {
  const message = event.data;
  if (message.statusEvent) {
    for (const listener of listeners) listener(message.value);
  } else {
    waiting.get(message.id)?.(message);
    waiting.delete(message.id);
  }
});
window.markdownTool = {
  getState: () => invoke('get-state'),
  saveConfig: value => invoke('save-config', value),
  exportClipboard: () => invoke('export-clipboard'),
  copyClipboardImage: () => invoke('copy-clipboard-image'),
  setCopyAfterExport: value => invoke('set-copy-after-export', value),
  setTheme: value => invoke('set-theme', value),
  setLanguage: value => invoke('set-language', value),
  chooseFolder: () => invoke('choose-folder'),
  openFolder: () => invoke('open-folder'),
  openRecent: () => invoke('open-recent'),
  ready: () => invoke('settings-ready'),
  onStatus: listener => listeners.push(listener)
};
})();

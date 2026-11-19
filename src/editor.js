(() => {
  const source = document.getElementById('markdown-source');
  const frame = document.getElementById('markdown-preview');
  const empty = document.getElementById('preview-empty');
  const status = document.getElementById('preview-status');
  const copy = document.getElementById('copy-editor-image');
  const save = document.getElementById('save-editor-image');
  const tabs = ['editor', 'settings'];
  let theme = 'light', language = 'zh-CN';
  let timer, revision = 0, pending = false, rendering = false, composing = false;
  let ready = false, visible = true, busy = false, exporting = false;
  const text = key => window.markdownTranslations[language][key];

  function updateButtons() {
    copy.disabled = save.disabled = busy || exporting || !source.value.trim();
  }

  function schedule(delay = 100) {
    ++revision;
    pending = true;
    clearTimeout(timer);
    empty.hidden = !!source.value.trim();
    updateButtons();
    if (ready && visible && !composing) timer = setTimeout(renderLatest, delay);
  }

  async function renderLatest() {
    if (!ready || !visible || composing || rendering || !pending) return;
    rendering = true;
    pending = false;
    const current = revision;
    try {
      const metrics = await frame.contentWindow.renderDocument(source.value, theme, language);
      if (current === revision) {
        status.textContent = text('previewHint');
        frame.dispatchEvent(new CustomEvent('preview-rendered', { detail: metrics }));
      }
    } catch (error) {
      if (current === revision) status.textContent = text('previewFailed') + error.message;
    } finally {
      rendering = false;
      if (pending && visible && !composing) { clearTimeout(timer); timer = setTimeout(renderLatest, 0); }
    }
  }

  function selectTab(name) {
    for (const tab of tabs) {
      const selected = name === tab;
      const button = document.getElementById(`${tab}-tab`);
      button.setAttribute('aria-selected', String(selected));
      button.tabIndex = selected ? 0 : -1;
      document.getElementById(`${tab}-panel`).hidden = !selected;
    }
    visible = name === 'editor';
    if (visible) schedule(0);
  }
  for (const name of tabs) {
    const tab = document.getElementById(`${name}-tab`);
    tab.addEventListener('click', () => selectTab(name));
    tab.addEventListener('keydown', event => {
      if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      const next = event.key === 'Home' ? 'editor' : event.key === 'End' ? 'settings' : tabs.find(value => value !== name);
      selectTab(next);
      document.getElementById(`${next}-tab`).focus();
    });
  }

  source.addEventListener('input', () => schedule());
  source.addEventListener('compositionstart', () => { composing = true; clearTimeout(timer); });
  source.addEventListener('compositionend', () => { composing = false; schedule(0); });
  frame.addEventListener('load', () => {
    ready = typeof frame.contentWindow.renderDocument === 'function';
    if (!ready) { status.textContent = text('previewFailed'); return; }
    frame.contentDocument.addEventListener('click', event => { if (event.target.closest('a')) event.preventDefault(); });
    schedule(0);
  });
  let previewWidth = 0;
  new ResizeObserver(entries => {
    const width = entries[0].contentRect.width;
    if (width > 0 && width !== previewWidth) { previewWidth = width; schedule(0); }
  }).observe(frame);

  async function exportImage(copyOnly) {
    if (copy.disabled || save.disabled) return;
    exporting = true;
    updateButtons();
    try {
      const response = await (copyOnly ? window.markdownTool.copyMarkdownImage(source.value) : window.markdownTool.exportMarkdown(source.value));
      if (!response.ok) throw new Error(response.message);
    } catch (error) {
      document.getElementById('status').textContent = error.message;
    } finally { exporting = false; updateButtons(); }
  }
  copy.addEventListener('click', () => exportImage(true));
  save.addEventListener('click', () => exportImage(false));

  window.markdownEditor = {
    configure(value) {
      const changed = (value.theme && value.theme !== theme) || (value.language && value.language !== language);
      if (value.theme) theme = value.theme;
      if (value.language) language = value.language;
      status.textContent = text('previewHint');
      if (changed) schedule(0);
    },
    setBusy(value) { busy = value; updateButtons(); }
  };
})();

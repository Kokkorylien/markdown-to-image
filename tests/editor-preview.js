(async () => {
  const source = document.getElementById('markdown-source');
  const frame = document.getElementById('markdown-preview');
  const tool = window.markdownTool;
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const preview = action => new Promise((resolve, reject) => {
    const timeout = setTimeout(() => { frame.removeEventListener('preview-rendered', done); reject(new Error('Live preview timed out')); }, 8000);
    const done = event => { clearTimeout(timeout); resolve(event.detail); };
    frame.addEventListener('preview-rendered', done, { once: true });
    action();
  });
  const enter = value => { source.value = value; source.dispatchEvent(new Event('input')); };
  const originalState = (await tool.getState()).value;
  const sample = '# 实时预览 / Live preview\n\n左侧修改 **Markdown**，右侧即时更新。\n\n| 功能 / Feature | 状态 / Status |\n| --- | --- |\n| 表格 / Tables | 支持 / Ready |\n| 公式 / Math | $E=mc^2$ |\n\n```javascript\nconst preview = "Hello, Markdown!";\nconsole.log(preview);\n```\n\n$$\\int_0^1 x^2 \\, dx = \\frac{1}{3}$$\n\n> 内容在本机渲染 / Rendered locally.\n\n![Example](https://example.invalid/image.png)\n\n<img src=x onerror="window.injected=true">';
  try {
    const metrics = await preview(() => enter(sample));
    assert(metrics.tables === 1 && metrics.formulas === 2 && metrics.highlighted > 0, 'Live Markdown features are missing');
    assert(source.value === sample, 'Rendering changed the source text');
    assert(!frame.contentDocument.querySelector('img') && !frame.contentWindow.injected, 'Raw HTML was executed');
    assert(!document.getElementById('copy-editor-image').disabled, 'Editor image actions are disabled');
    const panes = ['markdown-source', 'markdown-preview'].map(id => document.getElementById(id).getBoundingClientRect());
    assert(panes[0].right < panes[1].left && panes[0].width > 300 && panes[1].height > 300, 'The editor panes are not side by side');

    const started = performance.now();
    await preview(() => { for (let i = 0; i < 50; i++) enter(`# 最新编辑 ${i}`); });
    assert(frame.contentDocument.querySelector('h1').textContent === '最新编辑 49', 'Rapid edits left a stale preview');
    const rapidEditMs = Math.round(performance.now() - started);
    source.dispatchEvent(new CompositionEvent('compositionstart'));
    enter('# 中文输入完成');
    await new Promise(resolve => setTimeout(resolve, 150));
    assert(frame.contentDocument.querySelector('h1').textContent === '最新编辑 49', 'Preview interrupted an active IME composition');
    await preview(() => source.dispatchEvent(new CompositionEvent('compositionend')));
    assert(frame.contentDocument.querySelector('h1').textContent === '中文输入完成', 'IME completion did not refresh the preview');

    document.getElementById('settings-tab').click();
    assert(document.getElementById('editor-panel').hidden, 'The settings tab did not hide the editor');
    assert(source.value === '# 中文输入完成', 'Switching tabs discarded editor text');
    enter(sample);
    await preview(() => document.getElementById('editor-tab').click());
    assert(frame.contentDocument.querySelector('table'), 'Returning to the editor did not refresh pending text');
    for (const theme of ['light', 'dark']) {
      await preview(() => {
        const select = document.getElementById('theme');
        select.value = theme;
        select.dispatchEvent(new Event('change'));
        source.dispatchEvent(new Event('input'));
      });
      const doc = frame.contentDocument;
      assert(doc.documentElement.dataset.theme === theme, 'Preview did not follow the theme');
      assert(frame.contentWindow.getComputedStyle(doc.body).backgroundColor === (theme === 'dark' ? 'rgb(23, 25, 29)' : 'rgb(255, 255, 255)'), 'Preview theme colors are wrong');
    }
    for (const language of ['en', 'zh-CN']) {
      await preview(() => {
        const select = document.getElementById('language');
        select.value = language;
        select.dispatchEvent(new Event('change'));
        source.dispatchEvent(new Event('input'));
      });
      assert(frame.title === window.markdownTranslations[language].livePreview, 'Preview title was not translated');
      assert(source.placeholder === window.markdownTranslations[language].editorPlaceholder, 'Editor placeholder was not translated');
      assert(frame.contentDocument.querySelector('.image-placeholder').textContent.includes(language === 'en' ? 'external image' : '未载入'), 'Preview image placeholder was not translated');
      assert(source.value === sample, 'Changing language modified the source text');
    }
    await preview(() => enter(''));
    assert(document.getElementById('copy-editor-image').disabled && document.getElementById('save-editor-image').disabled, 'Empty editor actions must be disabled');
    assert(!frame.contentDocument.getElementById('content').textContent, 'Clearing the editor left old preview content');
    assert(!document.getElementById('preview-empty').hidden, 'The empty preview hint is missing');
    await preview(() => enter(sample));

    const buttonRequest = (method, id) => new Promise((resolve, reject) => {
      const original = tool[method];
      const timeout = setTimeout(() => { tool[method] = original; reject(new Error('Editor image action timed out')); }, 8000);
      tool[method] = value => {
        tool[method] = original;
        assert(value === sample, 'Image action did not use the current editor text');
        return original(value).then(result => { clearTimeout(timeout); resolve(result); return result; });
      };
      document.getElementById(id).click();
    });
    const copied = await buttonRequest('copyMarkdownImage', 'copy-editor-image');
    assert(copied.ok && copied.value.ok && copied.value.copied && !copied.value.output, 'Copy as image failed');
    // Allow the button handler to release its local exporting flag.
    await new Promise(resolve => setTimeout(resolve, 0));
    const saved = await buttonRequest('exportMarkdown', 'save-editor-image');
    assert(saved.ok && saved.value.ok && saved.value.output && saved.value.width === originalState.width, 'Save as image failed');
    assert(source.value === sample, 'Exporting discarded editor text');
    await tool.setTheme(originalState.theme);
    await tool.setLanguage(originalState.language);
    await preview(() => { window.markdownEditor.configure(originalState); source.dispatchEvent(new Event('input')); });
    chrome.webview.postMessage({ channel: 'verification-result', value: { ok: true, rapidEditMs, tables: metrics.tables, formulas: metrics.formulas, output: saved.value.output } });
  } catch (error) {
    chrome.webview.postMessage({ channel: 'verification-result', value: { ok: false, message: error.stack || error.message } });
  }
})();

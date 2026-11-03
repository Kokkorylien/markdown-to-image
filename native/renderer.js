const MarkdownIt = require('markdown-it');
const hljs = require('highlight.js');
const katex = require('katex');
const texmath = require('markdown-it-texmath');
const md = new MarkdownIt({
  html: false, linkify: true, breaks: true,
  highlight(code, language) {
    const text = language && hljs.getLanguage(language)
      ? hljs.highlight(code, { language, ignoreIllegals: true }).value
      : md.utils.escapeHtml(code);
    const label = language ? `<div class="code-language">${md.utils.escapeHtml(language)}</div>` : '';
    return `<pre class="hljs">${label}<code>${text}</code></pre>`;
  }
}).use(texmath, {
  engine: katex, delimiters: ['dollars', 'brackets'],
  katexOptions: { throwOnError: false, trust: false, strict: 'ignore', maxSize: 20, maxExpand: 1000 }
});
let language = 'zh-CN';
const text = key => window.markdownTranslations[language][key];
md.renderer.rules.image = (tokens, index) => `<span class="image-placeholder">${md.utils.escapeHtml(text('imagePlaceholder').replace('{0}', tokens[index].content || text('image')))}</span>`;

window.renderMarkdown = async (markdown, theme, id, selectedLanguage) => {
  try {
    language = selectedLanguage;
    document.documentElement.lang = selectedLanguage;
    document.documentElement.dataset.theme = theme;
    const sheet = document.getElementById('code-style');
    const codeStyle = theme === 'dark' ? 'github-dark.css' : 'github.css';
    const styleReady = sheet.getAttribute('href') === codeStyle ? Promise.resolve() : new Promise((resolve, reject) => {
      sheet.onload = resolve;
      sheet.onerror = () => reject(new Error(text('codeThemeFailed')));
      sheet.href = codeStyle;
    });
    const article = document.getElementById('content');
    article.innerHTML = md.render(markdown);
    await document.fonts.ready;
    await styleReady;
    for (const element of document.querySelectorAll('table, .katex-display')) {
      const style = getComputedStyle(element.parentElement);
      const available = element.parentElement.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
      const actual = Math.max(element.scrollWidth, element.getBoundingClientRect().width);
      if (actual > available && available > 0) element.style.zoom = String(available / actual);
    }
    // Fonts and layout are the only prerequisites for the screenshot.
    const metrics = {
      id, theme: document.documentElement.dataset.theme,
      background: getComputedStyle(document.body).backgroundColor,
      width: document.documentElement.clientWidth,
      height: Math.ceil(article.getBoundingClientRect().height),
      scrollWidth: document.documentElement.scrollWidth,
      codeBlocks: document.querySelectorAll('pre code').length,
      highlighted: document.querySelectorAll('pre .hljs-keyword').length,
      tables: document.querySelectorAll('table').length,
      formulas: document.querySelectorAll('.katex').length
    };
    chrome.webview.postMessage(metrics);
  } catch (error) { chrome.webview.postMessage({ id, error: error.message }); }
};

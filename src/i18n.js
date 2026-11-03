(() => {
  const catalogs = window.markdownTranslations;
  let language = 'zh-CN';
  window.markdownI18n = {
    text: key => catalogs[language][key],
    apply(value) {
      language = value;
      document.documentElement.lang = value;
      document.title = catalogs[value].settingsTitle;
      for (const node of document.querySelectorAll('[data-i18n]')) node.textContent = catalogs[value][node.dataset.i18n];
    }
  };
})();

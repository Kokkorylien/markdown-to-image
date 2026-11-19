# v1.1.0 · 编辑与实时预览 / Editor and live preview

新增左右分栏编辑器：左侧输入或粘贴纯文本 Markdown，右侧自动显示渲染结果，支持表格、代码高亮与数学公式。

- 双击托盘图标打开“编辑预览”，切换页签可进入原有的快捷转换与设置。
- “复制为图片”直接转换当前编辑内容，随后可用 **Ctrl+V** 粘贴；“保存为图片”使用设置中的保存位置、图片宽度和自动复制选项。
- 预览与界面同步切换简体中文 / English、浅色 / 深色主题。
- 连续输入合并更新，中文输入法组合输入结束后刷新；切换页签保留文字。关闭窗口会清空编辑内容，请先复制原文或保存图片。
- 预览和导出复用本地 Markdown 渲染；原有 **Ctrl+Alt+V** 图片粘贴及 **Ctrl+Alt+M** 保存快捷键继续可用。

## 下载与更新

下载 `MarkdownToImage-v1.1.0-win32-x64.zip`（约 **1 MB**，解压约 **2.8 MB**），解压整个文件夹，运行 `MarkdownToImage.exe`。从旧版更新时，先从托盘退出旧程序，再替换完整文件夹；快捷键、语言、主题和保存位置会保留。

需要 **Windows x64**、**.NET Framework 4.8** 和 **Microsoft Edge WebView2 Runtime**，无需 Node.js。缺少 WebView2 时，请先安装[微软官方运行时](https://go.microsoft.com/fwlink/p/?LinkId=2124703)。仅提供轻量版，不附带浏览器运行时或 Electron。

内容在本机处理，不调用 AI、不上传剪贴板。外部图片显示占位文字，原始 HTML 按文字显示。预览按窗口宽度排版；导出图片使用设置中的 720 / 1080 / 1440 px 宽度，因此换行可能不同。

已验证实际 WebView2 中的实时预览、快速连续编辑、输入法组合事件、页签文字保留、中英文及深浅色切换、编辑内容复制与保存，以及约 3.9 万像素长图底部完整性。`SHA256SUMS.txt` 提供压缩包的 SHA-256 校验值。

## English

New split editor: type or paste plain-text Markdown on the left and see the rendered result update on the right, including tables, syntax highlighting, and math.

- Double-click the tray icon to open **Editor and preview**. The **Shortcuts and settings** tab contains the existing clipboard actions and preferences.
- **Copy as image** converts the current editor text for **Ctrl+V**. **Save as image** uses the configured folder, image width, and automatic copy preference.
- The preview follows the selected English / 简体中文 language and light / dark theme.
- Rapid edits are combined, and preview updates wait for IME composition to finish. Switching tabs keeps your text. Closing the window clears the editor, so copy the source or save an image first.
- Preview and image export share the local Markdown renderer. Existing **Ctrl+Alt+V** image paste and **Ctrl+Alt+M** save shortcuts remain available.

Download `MarkdownToImage-v1.1.0-win32-x64.zip` (about **1 MB**, about **2.8 MB** extracted), extract the entire folder, and run `MarkdownToImage.exe`. To upgrade, quit the old app from its tray menu before replacing the entire app folder. Your shortcuts, language, theme, and save location are preserved.

Requires **Windows x64**, **.NET Framework 4.8**, and **Microsoft Edge WebView2 Runtime**. Node.js is not required. If WebView2 is missing, install the [official Microsoft runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703). Only the lightweight package is provided, without a bundled browser runtime or Electron.

Content stays local, with no AI calls or clipboard uploads. External images appear as placeholders and raw HTML is displayed as text. Preview wraps to the window width; exported images use the configured 720 / 1080 / 1440 px width, so line wrapping may differ.

Verified in actual WebView2: live rendering, rapid edits, IME composition events, text retention across tabs, both languages and themes, editor image copy and save, and complete output at approximately 39,000 px tall. `SHA256SUMS.txt` contains the ZIP checksum.

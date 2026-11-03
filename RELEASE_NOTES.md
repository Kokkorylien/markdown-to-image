# v1.0.0

复制 Markdown，在目标输入框按 **Ctrl+Alt+V**，直接粘贴为图片。图片会保留在剪贴板，之后按 **Ctrl+V** 可以继续粘贴。

采用原生 C# / WinForms + 系统 WebView2，去除随程序分发的 Electron、Chromium 和 Node.js。

- 支持标题、列表、引用、表格、代码高亮和数学公式。
- 支持完整长图、浅色/深色主题、720/1080/1440 px 宽度。
- 主题同时应用于设置界面和新生成的图片，切换立即保存；已有剪贴板图片保留原样。
- 按 **Ctrl+Alt+M** 保存为 PNG，支持调整保存目录与全局快捷键。
- 提供“转为图片并复制”按钮和保存后自动复制开关。
- 简体中文 / English 可选，切换立即保存；提供双语界面、托盘菜单、提示及 README。
- 转换在本机完成，不调用 AI，不上传剪贴板，不加载外部图片。

## 下载与运行

下载 `MarkdownToImage-v1.0.0-win32-x64.zip`，解压整个文件夹，运行 `MarkdownToImage.exe`。适用于 Windows x64，无需安装 Node.js；使用系统的 .NET Framework 4.8 和 Microsoft Edge WebView2 Runtime。

压缩包约 **1 MB**，解压约 **2.8 MB**。本次仅发布轻量版，不提供 Electron 大体积版本。若系统缺少运行时，请先安装[微软官方 WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)，再启动程序。

程序常驻托盘；右键托盘图标可打开设置、保存目录或退出。首次启动会显示设置。快捷键被占用时可在设置中更换。

自动粘贴需要目标应用支持图片粘贴。转换期间保持目标输入框不变。外部图片显示占位文字，排版不保证与聊天客户端完全一致。

## 验证

- 已实测：转换前 Ctrl+V 粘贴 Markdown；Ctrl+Alt+V 转换并自动粘贴图片；随后 Ctrl+V 再次粘贴同一图片。
- 已检查表格、代码高亮、公式、浅色/深色，以及约 3.9 万像素长图底部完整性。
- 已验证复制图片、仅保存、设置持久化和两个快捷键分别注册。

`SHA256SUMS.txt` 提供 Windows 压缩包的 SHA-256 校验值。

## English

Copy Markdown and press **Ctrl+Alt+V** in an image-capable input to convert and paste it as an image. The image stays in the clipboard for subsequent **Ctrl+V** pastes.

- Native C# / WinForms app using the installed WebView2 Runtime; no bundled Electron, Chromium, or Node.js.
- Select **English** or **简体中文** in settings. The choice is saved immediately and applies to the UI, tray menu, notifications, and the app's own error messages. Both READMEs are included in the repository.
- Tables, syntax highlighting, local math rendering, long images, light/dark themes, and 720/1080/1440 px widths.
- **Ctrl+Alt+M** saves a PNG. Saving, copying, automatic copying after saving, and both shortcuts are configurable.
- Local conversion with no AI calls, clipboard uploads, or remote image loading. Existing clipboard images keep their original theme.

Download `MarkdownToImage-v1.0.0-win32-x64.zip` (about **1 MB**, about **2.8 MB** extracted), extract the entire folder, and run `MarkdownToImage.exe`. Keep the supporting files. Windows x64, .NET Framework 4.8, and Microsoft Edge WebView2 Runtime are required. If WebView2 is missing, install the [official Microsoft runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) first.

This release only provides the lightweight package. `SHA256SUMS.txt` contains its checksum. Validation covers the actual paste flow, long image completeness, rendered theme colors, native settings, and Chinese/English switching and persistence.

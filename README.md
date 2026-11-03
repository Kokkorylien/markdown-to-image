# Markdown 快捷转图片

Windows 原生轻量托盘工具：复制回复，按 `Ctrl+Alt+V`，直接粘贴为图片。不调用 AI，也不上传剪贴板。

使用 C# / WinForms 和系统已安装的 WebView2 排版，不附带 Electron、Chromium 或 Node.js。运行需要 Windows x64、.NET Framework 4.8 和 Microsoft Edge WebView2 Runtime；Windows 10/11 通常已安装这些组件。缺少 WebView2 时程序会提示安装。

缺少运行时时，请先安装[微软官方 WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)，再启动程序。本项目的 Release 只提供轻量软件包，不附带浏览器运行时。

免安装压缩包约 **1 MB**，解压后约 **2.8 MB**。这些数字不包含系统已有的 WebView2 运行时；渲染时仍会启动 WebView2 子进程。

## 使用

### 下载免安装版

从 [GitHub Releases](https://github.com/Kokkorylien/markdown-to-image/releases/latest) 下载 `MarkdownToImage-v1.0.0-win32-x64.zip`，解压整个文件夹，双击其中的 `MarkdownToImage.exe` 即可。适用于 Windows x64，不需要安装 Node.js。

### 从源码运行

构建需要 Windows x64、.NET Framework 4.8，以及 Node.js 22.12 或更新版本。双击 `启动.cmd` 可安装构建依赖并启动。首次构建需要联网下载 WebView2 SDK；已有本地打包版本时会直接启动程序。

1. 在聊天中点击回复的“复制”，或复制任意 Markdown 文本。
2. 在支持图片的目标输入框按 **Ctrl+Alt+V**，松开快捷键，图片会自动粘贴。
3. 图片会留在剪贴板，之后按 **Ctrl+V** 可继续粘贴。转换前 Ctrl+V 仍粘贴原来的 Markdown 文字。

图片粘贴操作不额外保存文件。需要文件时，使用保存快捷键（默认 **Ctrl+Alt+M**）或“保存剪贴板为图片”按钮，图片默认保存在 Windows **图片\Markdown图片**。

右键托盘图标可打开设置、保存目录或退出。关闭设置窗口后仍在后台运行。设置中可改快捷键、保存位置、图片宽度和主题。

如果快捷键被其他程序占用，设置窗口会提示更换。图片粘贴和保存快捷键可以分别设置。

“界面与图片主题”切换后立即保存，同时改变设置窗口和新生成图片的配色。剪贴板里已有的图片保留原样；需要改成另一主题时，先重新复制 Markdown，再转换。

**导出后自动复制图片**默认开启，切换后立即生效并记住设置。使用快捷键或“保存剪贴板为图片”后，图片会成为剪贴板当前内容，在支持图片的应用里按 **Ctrl+V** 即可粘贴。关闭开关时，保存操作保留原来的剪贴板文字。

点击 **转为图片并复制** 可直接生成图片到剪贴板，无需保存文件，此按钮始终复制图片，不受开关影响。托盘菜单也提供同一入口。

转换使用文字剪贴板；若来源仅复制纯文本，已丢失的 Markdown 格式无法恢复。图片写入剪贴板后，如需再次转换，请重新复制 Markdown 文字。

重复按图片粘贴快捷键时，若剪贴板已经是图片，直接粘贴当前图片。转换期间请保持目标输入框不变；若切换窗口，会保留复制好的图片，提示手动 Ctrl+V。普通权限程序不能向管理员权限窗口自动发送粘贴键。

## 排版

- 标题、列表、引用、链接、表格、常见代码语言高亮。
- `$...$`、`$$...$$`、`\(...\)`、`\[...\]` 数学公式。
- 高度按内容生成，长代码行自动换行，过宽表格和独立公式缩放以保留完整内容。
- 提供浅色与深色主题；采用独立排版，不保证与聊天客户端逐像素相同。
- 外部图片显示占位文字；原始 HTML 按文字显示。转换期间不加载远程资源。
- 图片特别长时，图片查看器或分享平台可能有自己的尺寸限制。

## 开发与打包

```powershell
npm.cmd ci
npm.cmd run build
npm.cmd start
npm.cmd test
npm.cmd run pack
```

打包结果位于 `release\MarkdownToImage-light-win32-x64\`，并生成版本 ZIP 和 `SHA256SUMS.txt`。整个文件夹一起保留或复制到其他 Windows x64 电脑，双击其中 `MarkdownToImage.exe` 即可，无需另装 Node.js。

界面与排版脚本由 esbuild 打包；C# 程序由 Windows 自带的 .NET Framework C# 编译器构建。WebView2 SDK 版本固定为 `1.0.4314.35`，缓存位于 `.build/`。

也可从文件直接导出（不会覆盖同名文件）：

```powershell
.\release\MarkdownToImage-light-win32-x64\MarkdownToImage.exe --export .\tests\sample.md --output .\sample.png
```

文件导出同样使用当前保存的图片宽度和主题。

设置保存在 `%APPDATA%\MarkdownToImage\settings.json`。程序不默认开机启动。

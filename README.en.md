# Markdown to Image

[简体中文](README.md) | English

A lightweight native Windows tray app. Copy Markdown, then press **Ctrl+Alt+V** to convert it into an image and paste it at the current input. Everything is processed locally, without AI calls or clipboard uploads.

The app uses C# / WinForms and the system's installed WebView2 Runtime. It does not bundle Electron, Chromium, or Node.js. The portable download is about **1 MB**, or **2.8 MB** extracted, excluding the shared system runtime. WebView2 processes still run while rendering.

## Download and run

Download `MarkdownToImage-v1.0.0-win32-x64.zip` from [GitHub Releases](https://github.com/Kokkorylien/markdown-to-image/releases/latest), extract the entire folder, and run `MarkdownToImage.exe`. Keep all files in the extracted folder.

Requires **Windows x64**, **.NET Framework 4.8**, and **Microsoft Edge WebView2 Runtime**. Windows 10/11 commonly have these components installed. If WebView2 is missing, install the [official Microsoft runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703), then start the app again. This release contains only the lightweight app; no bundled browser version is provided.

1. Copy a reply or any Markdown text.
2. Focus an input that accepts images, press **Ctrl+Alt+V**, and release the keys. The image is pasted automatically.
3. The image remains in the clipboard. Use **Ctrl+V** to paste it again. Before conversion, Ctrl+V still pastes the original Markdown text.

The image paste shortcut does not save a file. To save a PNG, press **Ctrl+Alt+M** or click **Save clipboard as PNG**. The default folder is **Pictures\Markdown Images** on new English installations. Existing save locations are preserved.

Right-click the tray icon to open settings, open the save folder, or quit. Closing the settings window keeps the app running. If a shortcut is already in use, choose another combination in settings.

## Language and theme

Choose **English** or **简体中文** in the **Language / 语言** selector. The choice is saved immediately and applies to the settings page, tray menu, notifications, and the app's own error messages. The first launch follows the Windows display language, using English for languages other than Chinese. Markdown content is not translated.

The **App and image theme** selector applies light or dark colors to settings and newly generated images. Theme changes are saved immediately. Existing clipboard images remain unchanged; copy the original Markdown again before converting with another theme.

**Copy image after saving** is enabled by default. Turn it off to save a PNG while keeping the original clipboard text. The **Convert and copy image** button always puts an image in the clipboard and does not create a file.

When the clipboard already contains an image, Ctrl+Alt+V pastes that image directly. To convert different text, copy Markdown again. Keep the intended input active during conversion. A normal-permission process cannot automatically send paste keys to an administrator-permission window.

## Rendering

- Headings, lists, quotes, links, tables, and syntax highlighting for common code languages.
- Math with `$...$`, `$$...$$`, `\(...\)`, and `\[...\]`, using local KaTeX assets.
- Long images grow with the content. Long code lines wrap; oversized tables and display equations scale to fit.
- Light and dark themes, with 720, 1080, and 1440 px output widths.
- External images become placeholders. Raw HTML is displayed as text. Rendering does not load remote resources.
- The layout may differ from the original chat app. Image viewers and sharing services can have their own size limits.

Conversion uses clipboard text. Formatting that the source application removed when copying cannot be recovered.

## Build from source

Build on Windows x64 with .NET Framework 4.8 and Node.js **22.12 or later**:

```powershell
npm.cmd ci
npm.cmd run build
npm.cmd start
npm.cmd test
npm.cmd run pack
```

You can also run `启动.cmd` from a checkout. It installs build dependencies if needed and starts the app. End users of the portable ZIP do not need Node.js.

The build uses esbuild for local JavaScript and the Windows .NET Framework C# compiler for the native app. The pinned WebView2 SDK version is `1.0.4314.35`; the first build downloads it into `.build/`. The output is `release\MarkdownToImage-light-win32-x64\`. Packaging also creates the versioned ZIP and `SHA256SUMS.txt`.

Close the running app before rebuilding its executable. Tests cover rendering, long image completeness, actual theme colors, the WebView2 settings bridge, and both UI languages.

To export a Markdown file without overwriting an existing file:

```powershell
.\release\MarkdownToImage-light-win32-x64\MarkdownToImage.exe --export .\tests\sample.md --output .\sample.png
```

File exports use the currently saved width and theme. Settings are stored in `%APPDATA%\MarkdownToImage\settings.json`. The app does not enable startup with Windows by default.

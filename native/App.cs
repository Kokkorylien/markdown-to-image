using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--quit") { Native.CloseExisting(); return; }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        bool created;
        using (var mutex = new Mutex(true, "Local\\MarkdownToImage.Light", out created))
        {
            if (!created && args.Length == 0) { Native.ShowExisting(); return; }
            using (var app = new App(args)) Application.Run(app);
        }
    }
}

internal static class Json
{
    static readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    public static string Encode(object value) { return serializer.Serialize(value); }
    public static Dictionary<string, object> Decode(string value) { return serializer.Deserialize<Dictionary<string, object>>(value); }
}

internal sealed class App : Form
{
    const string Origin = "https://markdown.local/";
    readonly string[] args;
    readonly string root = AppDomain.CurrentDomain.BaseDirectory;
    readonly string profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MarkdownToImage");
    readonly Dictionary<int, TaskCompletionSource<Dictionary<string, object>>> renders = new Dictionary<int, TaskCompletionSource<Dictionary<string, object>>>();
    Dictionary<string, object> config;
    CoreWebView2Environment environment;
    WebView2 renderer;
    Form settings;
    WebView2 settingsView;
    Label settingsLoading;
    TaskCompletionSource<bool> settingsReady;
    NotifyIcon tray;
    string status = "准备就绪", recentFile;
    bool busy, exiting;
    DataObject integrationClipboard;
    int renderId;

    public App(string[] values)
    {
        args = values;
        Text = "MarkdownToImage renderer";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
        ClientSize = new Size(1080, 800);
        FormBorderStyle = FormBorderStyle.None;
        Load += async delegate { await Start(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (busy && !exiting) { e.Cancel = true; SetStatus("正在转换，完成后再退出。"); }
        };
    }

    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams {
        get { var p = base.CreateParams; p.ExStyle |= 0x08000080; return p; }
    }

    async Task Start()
    {
        try
        {
            bool first = !File.Exists(Path.Combine(profile, "settings.json"));
            config = new Dictionary<string, object> {
                {"hotkey", "Control+Alt+M"}, {"pasteHotkey", "Control+Alt+V"},
                {"outputDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Markdown图片")},
                {"width", 1080}, {"theme", "light"}, {"copyAfterExport", true}
            };
            if (!first) foreach (var pair in Json.Decode(File.ReadAllText(Path.Combine(profile, "settings.json")))) config[pair.Key] = pair.Value;
            Directory.CreateDirectory(profile);
            if (first) Save();
            environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(profile, "WebView2"), new CoreWebView2EnvironmentOptions("--disable-backgrounding-occluded-windows --disable-renderer-backgrounding"));
            renderer = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(renderer);
            await Setup(renderer);
            renderer.CoreWebView2.WebMessageReceived += RenderMessage;
            await Navigate(renderer, Origin + "export.html");
            if (args.Length > 0 && args[0] == "--verify") { await Verify(args[1]); return; }
            if (args.Length > 0 && args[0] == "--verify-settings") { await VerifySettings(args[1]); return; }
            if (args.Length > 0 && args[0] == "--export") {
                int index = Array.IndexOf(args, "--output");
                if (index < 0 || index + 1 == args.Length) throw new Exception("Usage: --export input.md --output output.png");
                var result = await Render(File.ReadAllText(args[1]), Convert.ToInt32(config["width"]), Convert.ToString(config["theme"]));
                WriteImage(args[index + 1], result.Png);
                Exit(); return;
            }
            CreateTray();
            var failed = new List<string>();
            for (int id = 1; id <= 2; id++) if (!Native.Register(Handle, id, Convert.ToString(config[id == 1 ? "hotkey" : "pasteHotkey"]))) failed.Add(Convert.ToString(config[id == 1 ? "hotkey" : "pasteHotkey"]));
            if (failed.Count > 0) { SetStatus("快捷键被占用：" + string.Join("、", failed)); ShowSettings(); }
            else if (first) ShowSettings();
            if (args.Length > 0 && args[0] == "--integration") await Integration(args[1], args[2]);
        }
        catch (WebView2RuntimeNotFoundException) {
            MessageBox.Show("轻量版需要 Microsoft Edge WebView2 Runtime。请安装该运行时后重试：https://go.microsoft.com/fwlink/p/?LinkId=2124703", "缺少 WebView2", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Environment.ExitCode = 1; Exit();
        }
        catch (Exception error) {
            if (args.Length > 0) File.WriteAllText(Path.Combine(root, "verify-error.txt"), error.ToString());
            else MessageBox.Show(error.Message, "启动失败");
            Environment.ExitCode = 1; Exit();
        }
    }


    async Task Setup(WebView2 view, bool inlineTest = false)
    {
        await view.EnsureCoreWebView2Async(environment);
        var core = view.CoreWebView2;
        core.SetVirtualHostNameToFolderMapping("markdown.local", Path.Combine(root, "assets"), CoreWebView2HostResourceAccessKind.DenyCors);
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; };
        core.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs e) { if (!e.Uri.StartsWith(Origin) && e.Uri != "about:blank" && !(inlineTest && e.Uri.StartsWith("data:text/html"))) e.Cancel = true; };
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += delegate(object sender, CoreWebView2WebResourceRequestedEventArgs e) {
            if (!e.Request.Uri.StartsWith(Origin) && !e.Request.Uri.StartsWith("data:") && e.Request.Uri != "about:blank") e.Response = environment.CreateWebResourceResponse(new MemoryStream(), 403, "Blocked", "");
        };
    }

    static async Task Navigate(WebView2 view, string url)
    {
        var ready = new TaskCompletionSource<bool>();
        EventHandler<CoreWebView2NavigationCompletedEventArgs> handler = null;
        handler = delegate(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            view.CoreWebView2.NavigationCompleted -= handler;
            if (e.IsSuccess) ready.TrySetResult(true); else ready.TrySetException(new Exception("页面加载失败：" + e.WebErrorStatus));
        };
        view.CoreWebView2.NavigationCompleted += handler;
        view.CoreWebView2.Navigate(url);
        await ready.Task;
    }

    void RenderMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var data = Json.Decode(e.WebMessageAsJson);
        TaskCompletionSource<Dictionary<string, object>> completion;
        int id = Convert.ToInt32(data["id"]);
        if (!renders.TryGetValue(id, out completion)) return;
        renders.Remove(id);
        if (data.ContainsKey("error")) completion.TrySetException(new Exception(Convert.ToString(data["error"]))); else completion.TrySetResult(data);
    }

    sealed class ImageResult { public byte[] Png; public Dictionary<string, object> Metrics; }
    async Task<ImageResult> Render(string markdown, int width, string theme)
    {
        var timer = Stopwatch.StartNew();
        await renderer.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", Json.Encode(new { width = width, height = 800, deviceScaleFactor = 1, mobile = false }));
        int id = ++renderId;
        var completion = new TaskCompletionSource<Dictionary<string, object>>();
        renders.Add(id, completion);
        try {
            await renderer.CoreWebView2.ExecuteScriptAsync("window.renderMarkdown(" + Json.Encode(markdown) + "," + Json.Encode(theme) + "," + id + ")");
            var metrics = await completion.Task;
            var screenshot = Json.Decode(await renderer.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", Json.Encode(new {
                format = "png", captureBeyondViewport = true,
                clip = new { x = 0, y = 0, width = width, height = Convert.ToInt32(metrics["height"]), scale = 1 }
            })));
            var png = Convert.FromBase64String(Convert.ToString(screenshot["data"]));
            metrics["milliseconds"] = timer.ElapsedMilliseconds;
            return new ImageResult { Png = png, Metrics = metrics };
        }
        finally { renders.Remove(id); }
    }

    void CreateTray()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("保存剪贴板为图片", null, async delegate { await ConvertClipboard(false, IntPtr.Zero); });
        menu.Items.Add("转为图片并复制", null, async delegate { await ConvertClipboard(true, IntPtr.Zero); });
        menu.Items.Add("设置与使用说明", null, delegate { ShowSettings(); });
        menu.Items.Add("打开保存目录", null, delegate { OpenFolder(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, delegate { if (!busy) Exit(); else SetStatus("正在转换，完成后再退出。"); });
        tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "Markdown 转图片 · 轻量版", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += delegate { ShowSettings(); };
    }

    Dictionary<string, object> State()
    {
        var data = new Dictionary<string, object>(config);
        data["status"] = status; data["exporting"] = busy; data["recentFile"] = recentFile; data["version"] = "1.0.0-light";
        return data;
    }
    void SetStatus(string value)
    {
        status = value;
        string tip = "Markdown 转图片 · " + value;
        if (tray != null) tray.Text = tip.Substring(0, Math.Min(63, tip.Length));
        if (settingsView != null && settingsView.CoreWebView2 != null) settingsView.CoreWebView2.PostWebMessageAsJson(Json.Encode(new { statusEvent = true, value = State() }));
    }
    void Notice(string title, string body)
    {
        if (tray != null) tray.ShowBalloonTip(2000, title, body, ToolTipIcon.Info);
    }
    void Save() { File.WriteAllText(Path.Combine(profile, "settings.json"), Json.Encode(config)); }
    void OpenFolder() { string folder = Convert.ToString(config["outputDir"]); Directory.CreateDirectory(folder); Process.Start(folder); }
    static void WriteImage(string file, byte[] png)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file)));
        using (var output = new FileStream(file, FileMode.CreateNew)) output.Write(png, 0, png.Length);
    }
    static void CopyImage(byte[] png)
    {
        using (var stream = new MemoryStream(png))
        using (var image = Image.FromStream(stream))
        using (var bitmap = new Bitmap(image)) {
            var data = new DataObject();
            data.SetData("PNG", false, new MemoryStream(png));
            data.SetData(DataFormats.Bitmap, true, bitmap);
            Clipboard.SetDataObject(data, true);
        }
    }

    async Task<Dictionary<string, object>> ConvertClipboard(bool copyOnly, IntPtr target)
    {
        if (busy) return new Dictionary<string, object> { {"ok", false}, {"message", "正在转换，请稍候。"} };
        busy = true;
        var timer = Stopwatch.StartNew();
        bool copied = false;
        string saved = null;
        SetStatus("正在生成图片…");
        try {
            string markdown = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(markdown)) {
                if (target != IntPtr.Zero && Clipboard.ContainsImage()) {
                    await Native.Paste(target); copied = true;
                    SetStatus("已粘贴剪贴板中的已有图片；更换主题后请重新复制 Markdown 转换");
                    return new Dictionary<string, object> { {"ok", true}, {"copied", true}, {"pasteSent", true} };
                }
                throw new Exception("剪贴板没有文字，请先复制 Markdown 或一条回复。");
            }
            var result = await Render(markdown, Convert.ToInt32(config["width"]), Convert.ToString(config["theme"]));
            if (!copyOnly) {
                saved = Path.Combine(Convert.ToString(config["outputDir"]), "Markdown_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png");
                WriteImage(saved, result.Png); recentFile = saved;
            }
            if (copyOnly || Convert.ToBoolean(config["copyAfterExport"])) { CopyImage(result.Png); copied = true; }
            if (target != IntPtr.Zero) await Native.Paste(target);
            string action = target != IntPtr.Zero ? "已复制并发送图片粘贴" : copyOnly ? "已复制图片，可按 Ctrl+V 粘贴" : copied ? "已保存并复制，可按 Ctrl+V 粘贴" : "已保存";
            SetStatus(action + " · " + result.Metrics["width"] + " × " + result.Metrics["height"] + " · " + timer.ElapsedMilliseconds + " ms");
            if (target == IntPtr.Zero) Notice("Markdown 图片", action);
            result.Metrics["ok"] = true; result.Metrics["copied"] = copied; result.Metrics["output"] = saved;
            return result.Metrics;
        }
        catch (Exception error) {
            string message = (copied ? "图片已复制：" : saved != null ? "图片已保存：" : "转换失败：") + error.Message;
            SetStatus(message); Notice("导出未完成", message);
            return new Dictionary<string, object> { {"ok", false}, {"message", message} };
        }
        finally { busy = false; SetStatus(status); }
    }

    async void ShowSettings()
    {
        if (settings != null) { settings.Show(); settings.Activate(); return; }
        bool dark = Convert.ToString(config["theme"]) == "dark";
        var background = dark ? Color.FromArgb(23, 25, 29) : Color.FromArgb(245, 247, 250);
        var window = new Form { Text = "Markdown 快捷转图片 · 轻量版", ClientSize = new Size(650, 870), MinimumSize = new Size(600, 650), StartPosition = FormStartPosition.CenterScreen, BackColor = background };
        var view = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = background };
        var loading = new Label { Text = "正在加载设置…", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = background, ForeColor = dark ? Color.FromArgb(165, 175, 188) : Color.FromArgb(92, 104, 120) };
        settings = window; settingsView = view;
        settingsLoading = loading; settingsReady = new TaskCompletionSource<bool>();
        window.Controls.Add(view); window.Controls.Add(loading); loading.BringToFront();
        window.FormClosed += delegate {
            view.Dispose();
            if (settings == window) { settingsView = null; settings = null; settingsLoading = null; }
        };
        try {
            window.Show(); Native.ShowWindow(window.Handle, 5);
            await Setup(view);
            if (window.IsDisposed) return;
            view.CoreWebView2.WebMessageReceived += SettingsMessage;
            await Navigate(view, Origin + "settings.html");
        }
        catch (Exception error) {
            if (!window.IsDisposed) { loading.Text = "设置加载失败：" + error.Message; loading.BringToFront(); }
        }
    }

    async void SettingsMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = Json.Decode(e.WebMessageAsJson);
        object value = null;
        try {
            string channel = Convert.ToString(message["channel"]);
            if (channel == "get-state") value = State();
            else if (channel == "settings-ready") {
                if (settingsLoading != null) { settingsLoading.Hide(); settingsLoading.Dispose(); settingsLoading = null; }
                if (settingsReady != null) settingsReady.TrySetResult(true);
            }
            else if (channel == "export-clipboard") value = await ConvertClipboard(false, IntPtr.Zero);
            else if (channel == "copy-clipboard-image") value = await ConvertClipboard(true, IntPtr.Zero);
            else if (channel == "set-copy-after-export") { config["copyAfterExport"] = Convert.ToBoolean(message["value"]); Save(); SetStatus("自动复制设置已保存"); value = State(); }
            else if (channel == "set-theme") { SetTheme(Convert.ToString(message["value"])); value = State(); }
            else if (channel == "save-config") { SaveSettings((Dictionary<string, object>)message["value"]); value = State(); }
            else if (channel == "choose-folder") {
                using (var picker = new FolderBrowserDialog { SelectedPath = Convert.ToString(config["outputDir"]), Description = "选择图片保存目录" }) {
                    if (picker.ShowDialog(settings) == DialogResult.OK) { config["outputDir"] = picker.SelectedPath; Save(); }
                }
                value = State();
            }
            else if (channel == "open-folder") OpenFolder();
            else if (channel == "open-recent" && recentFile != null) Process.Start(recentFile);
            else if (channel != "open-recent") throw new Exception("未知操作。");
            if (settingsView != null) settingsView.CoreWebView2.PostWebMessageAsJson(Json.Encode(new { id = message["id"], ok = true, value = value }));
        }
        catch (Exception error) {
            if (settingsView != null) settingsView.CoreWebView2.PostWebMessageAsJson(Json.Encode(new { id = message["id"], ok = false, message = error.Message }));
        }
    }

    void UpdateSettingsBackground()
    {
        bool dark = Convert.ToString(config["theme"]) == "dark";
        var background = dark ? Color.FromArgb(23, 25, 29) : Color.FromArgb(245, 247, 250);
        if (settings != null) settings.BackColor = background;
        if (settingsView != null) settingsView.DefaultBackgroundColor = background;
        if (settingsLoading != null) {
            settingsLoading.BackColor = background;
            settingsLoading.ForeColor = dark ? Color.FromArgb(165, 175, 188) : Color.FromArgb(92, 104, 120);
        }
    }

    void SetTheme(string theme)
    {
        if (theme != "light" && theme != "dark") throw new Exception("请选择有效主题。");
        var updated = new Dictionary<string, object>(config);
        updated["theme"] = theme;
        File.WriteAllText(Path.Combine(profile, "settings.json"), Json.Encode(updated));
        config = updated;
        UpdateSettingsBackground();
        SetStatus((theme == "dark" ? "深色" : "浅色") + "主题已应用，新生成的图片立即生效");
    }

    void SaveSettings(Dictionary<string, object> input)
    {
        int width = Convert.ToInt32(input["width"]);
        string theme = Convert.ToString(input["theme"]);
        if (width != 720 && width != 1080 && width != 1440) throw new Exception("请选择有效的图片宽度。");
        if (theme != "light" && theme != "dark") throw new Exception("请选择有效主题。");
        string hotkey = Convert.ToString(input["hotkey"]), paste = Convert.ToString(input["pasteHotkey"]);
        if (Native.Parse(hotkey).Equals(Native.Parse(paste))) throw new Exception("保存和粘贴快捷键不能相同。");
        string oldHotkey = Convert.ToString(config["hotkey"]), oldPaste = Convert.ToString(config["pasteHotkey"]);
        Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2);
        if (!Native.Register(Handle, 1, hotkey) || !Native.Register(Handle, 2, paste)) {
            Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2);
            Native.Register(Handle, 1, oldHotkey); Native.Register(Handle, 2, oldPaste);
            throw new Exception("快捷键被占用，请换一个组合。");
        }
        var before = new Dictionary<string, object>(config);
        config["hotkey"] = hotkey; config["pasteHotkey"] = paste; config["width"] = width; config["theme"] = theme;
        try { Save(); }
        catch {
            config = before;
            Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2);
            Native.Register(Handle, 1, oldHotkey); Native.Register(Handle, 2, oldPaste);
            throw;
        }
        UpdateSettingsBackground();
        SetStatus("设置已保存");
    }

    async Task Verify(string manifest)
    {
        var data = Json.Decode(File.ReadAllText(manifest));
        var results = new List<object>();
        foreach (Dictionary<string, object> test in (System.Collections.IEnumerable)data["cases"]) {
            var result = await Render(Convert.ToString(test["markdown"]), Convert.ToInt32(test["width"]), Convert.ToString(test["theme"]));
            string file = Convert.ToString(test["output"]);
            WriteImage(file, result.Png);
            result.Metrics["output"] = file;
            using (var stream = new MemoryStream(result.Png)) using (var image = Image.FromStream(stream)) {
                result.Metrics["imageWidth"] = image.Width; result.Metrics["imageHeight"] = image.Height;
                using (var bitmap = new Bitmap(image)) result.Metrics["cornerColor"] = bitmap.GetPixel(2, 2).ToArgb().ToString("X8");
                if (image.Height > 30000) {
                    using (var bitmap = new Bitmap(image)) {
                        int dark = 0;
                        for (int y = image.Height - 150; y < image.Height; y++) for (int x = 0; x < image.Width; x++) { var pixel = bitmap.GetPixel(x, y); if (pixel.R < 150 && pixel.G < 150 && pixel.B < 150) dark++; }
                        result.Metrics["bottomDarkPixels"] = dark;
                    }
                }
            }
            results.Add(result.Metrics);
        }
        File.WriteAllText(Convert.ToString(data["resultFile"]), Json.Encode(results));
        Exit();
    }

    async Task VerifySettings(string resultFile)
    {
        var view = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(245, 247, 250) };
        Controls.Add(view); view.BringToFront(); settingsView = view;
        settingsReady = new TaskCompletionSource<bool>();
        var saved = new TaskCompletionSource<Dictionary<string, object>>();
        await Setup(view);
        view.CoreWebView2.WebMessageReceived += delegate(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            var message = Json.Decode(e.WebMessageAsJson);
            if (Convert.ToString(message["channel"]) == "verification-result") saved.TrySetResult((Dictionary<string, object>)message["value"]);
            else SettingsMessage(sender, e);
        };
        await Navigate(view, Origin + "settings.html");
        await settingsReady.Task;
        var values = Json.Decode(await view.CoreWebView2.ExecuteScriptAsync("({status:document.getElementById('status').textContent,hotkey:document.getElementById('hotkey').value,pasteHotkey:document.getElementById('paste-hotkey').value,theme:document.getElementById('theme').value,outputDir:document.getElementById('output-dir').textContent,background:getComputedStyle(document.body).backgroundColor})"));
        if (Convert.ToString(values["hotkey"]) != Convert.ToString(config["hotkey"]) || Convert.ToString(values["status"]).Contains("undefined")) throw new Exception("设置页未正确读取原生配置。");
        var input = new { hotkey = config["hotkey"], pasteHotkey = config["pasteHotkey"], width = config["width"], theme = config["theme"] };
        await view.CoreWebView2.ExecuteScriptAsync("window.markdownTool.saveConfig(" + Json.Encode(input) + ").then(value=>chrome.webview.postMessage({channel:'verification-result',value}));");
        var response = await saved.Task;
        if (!Convert.ToBoolean(response["ok"])) throw new Exception("设置保存失败：" + response["message"]);
        values["savePassed"] = true;
        string originalTheme = Convert.ToString(config["theme"]);
        var themeChecks = new List<object>();
        await view.CoreWebView2.ExecuteScriptAsync("window.verifySetTheme=window.markdownTool.setTheme;window.markdownTool.setTheme=theme=>window.verifySetTheme(theme).then(value=>{chrome.webview.postMessage({channel:'verification-result',value});return value;});");
        try {
            foreach (string theme in new[] { originalTheme == "dark" ? "light" : "dark", originalTheme }) {
                saved = new TaskCompletionSource<Dictionary<string, object>>();
                await view.CoreWebView2.ExecuteScriptAsync("document.getElementById('theme').value=" + Json.Encode(theme) + ";document.getElementById('theme').dispatchEvent(new Event('change'));");
                var changed = await saved.Task;
                if (!Convert.ToBoolean(changed["ok"])) throw new Exception("主题切换失败。");
                string background = Json.Encode(theme == "dark" ? "rgb(23, 25, 29)" : "rgb(245, 247, 250)");
                string actual = await view.CoreWebView2.ExecuteScriptAsync("getComputedStyle(document.body).backgroundColor");
                if (actual != background || Convert.ToString(Json.Decode(File.ReadAllText(Path.Combine(profile, "settings.json")))["theme"]) != theme) throw new Exception("界面主题或保存的主题未同步更新。");
                var image = await Render("# 主题验证\n\n```javascript\nconst theme = '" + theme + "';\n```", 1080, Convert.ToString(config["theme"]));
                string corner;
                using (var stream = new MemoryStream(image.Png)) using (var bitmap = new Bitmap(stream)) corner = bitmap.GetPixel(2, 2).ToArgb().ToString("X8");
                if (corner != (theme == "dark" ? "FF17191D" : "FFFFFFFF")) throw new Exception("导出图片未使用当前主题。");
                File.WriteAllBytes(resultFile + "." + theme + ".image.png", image.Png);
                var page = Json.Decode(await view.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", "{\"format\":\"png\",\"captureBeyondViewport\":true}"));
                File.WriteAllBytes(resultFile + "." + theme + ".ui.png", Convert.FromBase64String(Convert.ToString(page["data"])));
                themeChecks.Add(new { theme = theme, background = actual, imageCornerColor = corner });
            }
        }
        finally { if (Convert.ToString(config["theme"]) != originalTheme) SetTheme(originalTheme); }
        values["themeChecks"] = themeChecks;
        var screenshot = Json.Decode(await view.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", "{\"format\":\"png\",\"captureBeyondViewport\":true}"));
        File.WriteAllBytes(resultFile + ".png", Convert.FromBase64String(Convert.ToString(screenshot["data"])));
        File.WriteAllText(resultFile, Json.Encode(values));
        view.Dispose(); settingsView = null;
        Exit();
    }

    async Task Integration(string input, string resultFile)
    {
        integrationClipboard = new DataObject();
        var original = Clipboard.GetDataObject();
        if (original != null) foreach (string format in original.GetFormats(false)) {
            object value = original.GetData(format, false);
            var stream = value as MemoryStream;
            var image = value as Image;
            if (stream != null) value = new MemoryStream(stream.ToArray());
            if (image != null) value = image.Clone();
            if (value != null) integrationClipboard.SetData(format, false, value);
        }
        Clipboard.SetText(File.ReadAllText(input));
        var test = new Form { Text = "Markdown 轻量版粘贴测试", ClientSize = new Size(800, 650), StartPosition = FormStartPosition.CenterScreen };
        var view = new WebView2 { Dock = DockStyle.Fill };
        test.Controls.Add(view); test.Show(); Native.ShowWindow(test.Handle, 5);
        await Setup(view, true);
        view.CoreWebView2.WebMessageReceived += delegate(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            var result = Json.Decode(e.WebMessageAsJson);
            if (Convert.ToInt32(result["count"]) < 3) return;
            result["clipboardHasImage"] = Clipboard.ContainsImage();
            result["clipboardHasText"] = Clipboard.ContainsText();
            result["status"] = status;
            File.WriteAllText(resultFile, Json.Encode(result));
            Clipboard.SetDataObject(integrationClipboard, true); integrationClipboard = null;
            BeginInvoke((Action)delegate { test.Close(); Exit(); });
        };
        test.FormClosed += delegate { view.Dispose(); if (integrationClipboard != null) { Clipboard.SetDataObject(integrationClipboard, true); integrationClipboard = null; Exit(); } };
        view.CoreWebView2.NavigateToString("<!doctype html><html><head><meta charset='utf-8'></head><body style='font:18px Segoe UI;margin:30px;background:#f5f7fa'><h2>轻量版：文字 → 图片 → 再次粘贴图片</h2><p>依次按 Ctrl+V、Ctrl+Alt+V、Ctrl+V。</p><div id='target' contenteditable='true' style='padding:20px;min-height:420px;background:white;border:1px solid #ddd'>点击这里开始</div><script>let count=0,history=[];document.getElementById('target').addEventListener('paste',e=>{e.preventDefault();const file=e.clipboardData.files[0];const entry={type:file?file.type:'text/plain',characters:e.clipboardData.getData('text/plain').length};history.push(entry);count++;if(!file){chrome.webview.postMessage({count,history});return;}const target=e.currentTarget;const reader=new FileReader();reader.onload=()=>{const img=new Image();img.style.maxWidth='100%';img.onload=()=>chrome.webview.postMessage({count,history,width:img.naturalWidth,height:img.naturalHeight});img.src=reader.result;target.replaceChildren(img);};reader.readAsDataURL(file);});</script></body></html>");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312) {
            int id = m.WParam.ToInt32();
            IntPtr target = id == 2 ? Native.GetForegroundWindow() : IntPtr.Zero;
            // The callback returns immediately; capture / clipboard work resumes on the UI context.
            BeginInvoke((Action)(async delegate { await ConvertClipboard(id == 2, target); }));
        }
        if (m.Msg == Native.ShowMessage) ShowSettings();
        if (m.Msg == Native.ExitMessage) Exit();
        base.WndProc(ref m);
    }
    void Exit()
    {
        exiting = true;
        if (integrationClipboard != null) { Clipboard.SetDataObject(integrationClipboard, true); integrationClipboard = null; }
        Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2);
        if (tray != null) { tray.Visible = false; tray.Dispose(); }
        if (settings != null) settings.Close();
        if (renderer != null) renderer.Dispose();
        Close();
    }
}

internal static class Native
{
    public static readonly int ShowMessage = RegisterWindowMessage("MarkdownToImage.ShowSettings");
    public static readonly int ExitMessage = RegisterWindowMessage("MarkdownToImage.Exit");
    [DllImport("user32.dll")] static extern int RegisterWindowMessage(string name);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, int message, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint count, Input[] inputs, int size);
    [StructLayout(LayoutKind.Sequential)] struct Keyboard { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Explicit, Size = 32)] struct InputData { [FieldOffset(0)] public Keyboard keyboard; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint type; public InputData data; }
    static Input Key(ushort key, bool up) { return new Input { type = 1, data = new InputData { keyboard = new Keyboard { vk = key, flags = up ? 2u : 0u } } }; }
    public static void ShowExisting() { PostMessage(FindWindow(null, "MarkdownToImage renderer"), ShowMessage, IntPtr.Zero, IntPtr.Zero); }
    public static void CloseExisting() { PostMessage(FindWindow(null, "MarkdownToImage renderer"), ExitMessage, IntPtr.Zero, IntPtr.Zero); }
    public static Tuple<uint, uint> Parse(string hotkey)
    {
        uint modifiers = 0, key = 0;
        foreach (string part in hotkey.Split('+')) {
            string token = part.Trim().ToUpperInvariant();
            if (token == "CTRL" || token == "CONTROL" || token == "COMMANDORCONTROL") modifiers |= 2;
            else if (token == "ALT") modifiers |= 1;
            else if (token == "SHIFT") modifiers |= 4;
            else if (token.Length == 1 && char.IsLetterOrDigit(token[0])) { if (key != 0) throw new Exception("快捷键只能包含一个字母、数字或功能键。"); key = token[0]; }
            else if (token.StartsWith("F")) { int number; if (!int.TryParse(token.Substring(1), out number) || number < 1 || number > 24 || key != 0) throw new Exception("快捷键无效。"); key = (uint)(111 + number); }
            else throw new Exception("快捷键无效，例如 Control+Alt+V。");
        }
        if (key == 0) throw new Exception("快捷键缺少字母、数字或功能键。");
        return Tuple.Create(modifiers, key);
    }
    public static bool Register(IntPtr handle, int id, string hotkey) { var value = Parse(hotkey); return RegisterHotKey(handle, id, value.Item1 | 0x4000, value.Item2); }
    public static async Task Paste(IntPtr target)
    {
        int[] keys = { 0x10, 0x11, 0x12, 0x5b, 0x5c, 0x56 };
        var timer = Stopwatch.StartNew();
        while (keys.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0)) {
            if (timer.ElapsedMilliseconds >= 3000) throw new Exception("请松开快捷键，再按 Ctrl+V 粘贴。");
            await Task.Delay(12);
        }
        if (target == IntPtr.Zero || GetForegroundWindow() != target) throw new Exception("输入窗口已切换，请在目标位置按 Ctrl+V 粘贴。");
        var inputs = new[] { Key(0x11, false), Key(0x56, false), Key(0x56, true), Key(0x11, true) };
        uint sent = SendInput(4, inputs, Marshal.SizeOf(typeof(Input)));
        if (sent != 4) {
            if (sent > 0) SendInput(2, new[] { Key(0x56, true), Key(0x11, true) }, Marshal.SizeOf(typeof(Input)));
            throw new Exception("当前应用未接受自动粘贴，请按 Ctrl+V 粘贴图片。");
        }
    }
}

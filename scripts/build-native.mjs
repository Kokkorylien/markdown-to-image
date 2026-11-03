import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { build } from 'esbuild';
import extract from 'extract-zip';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = path.join(root, 'release', 'MarkdownToImage-light-win32-x64');
const assets = path.join(output, 'assets');
const sdkVersion = '1.0.4314.35';
const sdk = path.join(root, '.build', `webview2-${sdkVersion}`);
const run = (command, args) => {
  const result = spawnSync(command, args, { cwd: root, stdio: 'inherit', windowsHide: true });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${command} exited ${result.status}`);
};
await fs.mkdir(assets, { recursive: true });
for (const name of ['verify-error.txt', 'integration.log', 'app.manifest']) await fs.rm(path.join(output, name), { force: true });
try { await fs.access(path.join(sdk, 'lib', 'net462', 'Microsoft.Web.WebView2.Core.dll')); }
catch {
  await fs.mkdir(sdk, { recursive: true });
  const archive = path.join(root, '.build', 'webview2.zip');
  const response = await fetch(`https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/${sdkVersion}/microsoft.web.webview2.${sdkVersion}.nupkg`);
  if (!response.ok) throw new Error(`WebView2 SDK download failed: ${response.status}`);
  await fs.writeFile(archive, Buffer.from(await response.arrayBuffer()));
  await extract(archive, { dir: sdk });
}
await build({ entryPoints: [path.join(root, 'native', 'renderer.js')], outfile: path.join(assets, 'renderer.js'), bundle: true, minify: true, platform: 'browser', target: 'chrome120' });
for (const name of ['export.html', 'bridge.js']) await fs.copyFile(path.join(root, 'native', name), path.join(assets, name));
for (const name of ['export.css', 'settings.css', 'settings.js']) await fs.copyFile(path.join(root, 'src', name), path.join(assets, name));
const settings = (await fs.readFile(path.join(root, 'src', 'settings.html'), 'utf8')).replace('<script src="settings.js">', '<script src="bridge.js"></script><script src="settings.js">');
await fs.writeFile(path.join(assets, 'settings.html'), settings);
await fs.copyFile(path.join(root, 'node_modules', 'katex', 'dist', 'katex.min.css'), path.join(assets, 'katex.min.css'));
const fontSource = path.join(root, 'node_modules', 'katex', 'dist', 'fonts');
const fontOutput = path.join(assets, 'fonts');
await fs.mkdir(fontOutput, { recursive: true });
for (const name of await fs.readdir(fontSource)) if (name.endsWith('.woff2')) await fs.copyFile(path.join(fontSource, name), path.join(fontOutput, name));
for (const name of await fs.readdir(fontOutput)) if (/\.(ttf|woff)$/.test(name)) await fs.rm(path.join(fontOutput, name));
for (const name of ['github.css', 'github-dark.css']) await fs.copyFile(path.join(root, 'node_modules', 'highlight.js', 'styles', name), path.join(assets, name));
for (const name of ['Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll']) await fs.copyFile(path.join(sdk, 'lib', 'net462', name), path.join(output, name));
await fs.copyFile(path.join(sdk, 'runtimes', 'win-x64', 'native', 'WebView2Loader.dll'), path.join(output, 'WebView2Loader.dll'));
const compiler = path.join(process.env.WINDIR || 'C:\\Windows', 'Microsoft.NET', 'Framework64', 'v4.0.30319', 'csc.exe');
run(compiler, [
  '/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/utf8output',
  `/out:${path.join(output, 'MarkdownToImage.exe')}`, `/win32manifest:${path.join(root, 'native', 'app.manifest')}`,
  '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll',
  `/reference:${path.join(output, 'Microsoft.Web.WebView2.Core.dll')}`, `/reference:${path.join(output, 'Microsoft.Web.WebView2.WinForms.dll')}`,
  path.join(root, 'native', 'App.cs')
]);
await fs.writeFile(path.join(output, 'MarkdownToImage.exe.config'), '<?xml version="1.0"?><configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup></configuration>');
const notices = ['Third-party components included in this distribution.\n'];
const lock = JSON.parse(await fs.readFile(path.join(root, 'package-lock.json'), 'utf8'));
for (const [directory, metadata] of Object.entries(lock.packages)) {
  if (!directory.startsWith('node_modules/') || metadata.dev) continue;
  const files = await fs.readdir(path.join(root, directory));
  for (const file of files.filter(name => /^licen[cs]e(\.(txt|md))?$/i.test(name))) {
    notices.push(`\n--- ${directory.slice('node_modules/'.length)} ${metadata.version} ---\n`);
    notices.push(await fs.readFile(path.join(root, directory, file), 'utf8'));
  }
}
for (const name of ['LICENSE.txt', 'NOTICE.txt']) notices.push(`\n--- Microsoft WebView2 SDK ${name} ---\n${await fs.readFile(path.join(sdk, name), 'utf8')}`);
await fs.writeFile(path.join(output, 'THIRD_PARTY_NOTICES.txt'), notices.join('\n'));
await fs.writeFile(path.join(output, '使用说明.txt'), 'Markdown 轻量版\r\n\r\n运行 MarkdownToImage.exe，复制 Markdown，在目标输入框按 Ctrl+Alt+V 即可粘贴图片。之后 Ctrl+V 可继续粘贴。\r\n默认 Ctrl+Alt+M 保存图片；右键托盘可更改快捷键、保存位置和主题。\r\n\r\n使用系统已安装的 Microsoft Edge WebView2 Runtime 和 .NET Framework 4.8，不附带浏览器内核。\r\n转换不调用 AI，不上传剪贴板。请保留此文件夹内的文件。\r\n');
console.log(path.join(output, 'MarkdownToImage.exe'));

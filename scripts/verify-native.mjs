import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';
import assert from 'node:assert/strict';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = path.join(root, 'test-output', 'native');
await fs.mkdir(output, { recursive: true });
const sample = await fs.readFile(path.join(root, 'tests', 'sample.md'), 'utf8');
const definitions = [
  { name: 'short', markdown: '一条简短的回复。', width: 1080, theme: 'light' },
  { name: 'light', markdown: sample, width: 1080, theme: 'light' },
  { name: 'dark', markdown: sample, width: 720, theme: 'dark' },
  { name: 'long', markdown: `# 长回复完整导出\n\n${Array.from({ length: 320 }, (_, i) => `## 第 ${i + 1} 段\n\n这是一段用于检查长图完整性的中文内容。表格、代码和整条回复都应当保留。\n\n`).join('')}\n\n结束标记：完整保存`, width: 1080, theme: 'light' }
];
const cases = definitions.map(value => ({ ...value, output: path.join(output, `${value.name}.png`) }));
for (const test of cases) await fs.rm(test.output, { force: true });
const resultFile = path.join(output, 'results.json');
const manifest = path.join(output, 'cases.json');
await fs.writeFile(manifest, JSON.stringify({ cases, resultFile }));
const exe = process.argv[2] || path.join(root, 'release', 'MarkdownToImage-light-win32-x64', 'MarkdownToImage.exe');
const child = spawn(exe, ['--verify', manifest], { cwd: root, windowsHide: true, stdio: 'inherit' });
const code = await new Promise((resolve, reject) => { child.on('error', reject); child.on('exit', resolve); });
assert.equal(code, 0, 'Native verification failed; see release/.../verify-error.txt');
const results = JSON.parse(await fs.readFile(resultFile, 'utf8'));
for (let i = 0; i < results.length; i++) {
  const actual = results[i], test = cases[i];
  assert.equal(actual.imageWidth, test.width);
  assert.equal(actual.imageHeight, actual.height);
  assert.equal(actual.scrollWidth, test.width);
  assert.equal(actual.theme, test.theme);
  assert.equal(actual.cornerColor, test.theme === 'dark' ? 'FF17191D' : 'FFFFFFFF', 'Rendered PNG theme differs from the selected theme');
  if (test.name === 'light' || test.name === 'dark') {
    assert.equal(actual.tables, 2); assert.equal(actual.codeBlocks, 2); assert.equal(actual.formulas, 4); assert.ok(actual.highlighted > 0);
  }
  if (test.name === 'short') assert.ok(actual.height < 250);
  if (test.name === 'long') { assert.ok(actual.height > 32767); assert.ok(actual.bottomDarkPixels > 100); }
  console.log(JSON.stringify({ test: test.name, ...actual, bytes: (await fs.stat(test.output)).size }));
}
console.log('PASS: native render, table, syntax highlighting, math, dark theme, long image bottom');
const settingsResult = path.join(output, 'settings-result.json');
await fs.rm(settingsResult, { force: true });
const settingsProcess = spawn(exe, ['--verify-settings', settingsResult], { cwd: root, windowsHide: true, stdio: 'inherit' });
const settingsCode = await new Promise((resolve, reject) => { settingsProcess.on('error', reject); settingsProcess.on('exit', resolve); });
assert.equal(settingsCode, 0, 'Native settings verification failed');
const settings = JSON.parse(await fs.readFile(settingsResult, 'utf8'));
assert.equal(settings.savePassed, true);
assert.equal(settings.status.includes('undefined'), false);
assert.ok(settings.hotkey && settings.pasteHotkey && settings.outputDir);
assert.equal(settings.themeChecks.length, 2);
assert.deepEqual(settings.themeChecks.map(value => value.theme).sort(), ['dark', 'light']);
console.log('PASS: real WebView2 settings page, initial native response, save settings, page screenshot');
console.log('PASS: theme dropdown updates UI, persisted config, and actual PNG colors');

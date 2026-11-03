import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const exe = path.join(root, 'release', 'MarkdownToImage-light-win32-x64', 'MarkdownToImage.exe');
try { await fs.access(exe); } catch { await import('./build-native.mjs'); }
const child = spawn(exe, [], { cwd: root, detached: true, windowsHide: true, stdio: 'ignore' });
child.unref();

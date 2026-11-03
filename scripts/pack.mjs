import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { zipSync } from 'fflate';
await import('./build-native.mjs');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const { version } = JSON.parse(await fs.readFile(path.join(root, 'package.json'), 'utf8'));
const folder = 'MarkdownToImage-light-win32-x64';
const source = path.join(root, 'release', folder);
const entries = {};
async function collect(directory) {
  for (const entry of await fs.readdir(directory, { withFileTypes: true })) {
    const file = path.join(directory, entry.name);
    if (entry.isDirectory()) await collect(file);
    else entries[`${folder}/${path.relative(source, file).replaceAll('\\', '/')}`] = new Uint8Array(await fs.readFile(file));
  }
}
await collect(source);
const name = `MarkdownToImage-v${version}-win32-x64.zip`;
const zip = zipSync(entries, { level: 9 });
await fs.writeFile(path.join(root, 'release', name), zip);
const digest = createHash('sha256').update(zip).digest('hex');
await fs.writeFile(path.join(root, 'release', 'SHA256SUMS.txt'), `${digest}  ${name}\n`);
const unpacked = Object.values(entries).reduce((total, bytes) => total + bytes.length, 0);
console.log(JSON.stringify({ archive: path.join(root, 'release', name), bytes: zip.length, unpackedBytes: unpacked, sha256: digest }));

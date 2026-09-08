import { mkdir, copyFile, cp } from 'node:fs/promises';

await mkdir('dist', { recursive: true });
for (const file of ['index.html', 'styles.css', 'app.js', 'salon.js']) {
  await copyFile(file, `dist/${file}`);
}
await cp('public', 'dist', { recursive: true });
await cp('licenses', 'dist/licenses', { recursive: true });
console.log('Website built in dist/. Upload this folder to your static hosting provider.');

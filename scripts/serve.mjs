import http from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import path from 'node:path';

const args = process.argv.slice(2);
const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const root = path.resolve(option('--dir', '.'));
const port = Number(option('--port', process.env.PORT || 5173));
const host = option('--host', '127.0.0.1');
const types = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.jpg': 'image/jpeg', '.png': 'image/png', '.svg': 'image/svg+xml', '.woff2': 'font/woff2', '.txt': 'text/plain; charset=utf-8', '.webp': 'image/webp' };

http.createServer(async (req, res) => {
  try {
    if (!['GET', 'HEAD'].includes(req.method)) {
      res.writeHead(405, { Allow: 'GET, HEAD' }).end();
      return;
    }
    const pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    // Serve only public website files, never project configuration or dotfiles.
    const allowed = /^\/(?:index\.html|styles\.css|app\.js|salon\.js|favicon\.svg|robots\.txt|images\/[a-z\d-]+\.(?:jpg|png|webp)|fonts\/[a-z\d-]+\.woff2)?$/i;
    if (!allowed.test(pathname)) { res.writeHead(404).end('Not found'); return; }
    const relative = pathname === '/' ? 'index.html' : pathname.slice(1);
    let file = path.join(root, relative);
    if (root === path.resolve('.') && /^(images\/|fonts\/|favicon\.svg|robots\.txt)/.test(relative)) file = path.join(root, 'public', relative);
    if (!(await stat(file)).isFile()) throw new Error('Not a file');
    const body = await readFile(file);
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-cache', 'X-Content-Type-Options': 'nosniff' });
    res.end(req.method === 'HEAD' ? undefined : body);
  } catch { res.writeHead(404).end('Not found'); }
}).listen(port, host, () => console.log(`Shalu's Salon is ready at http://${host}:${port}`));

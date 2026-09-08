# Shalu’s Salon

A responsive salon website with cream and burgundy styling, locally hosted photographs and fonts, WhatsApp appointment requests, a filterable inspiration gallery, business hours in Malaysia time, directions, and accessible dialogs.

## Run locally

Requires Node.js 20 or later. No dependency installation is needed.

```sh
npm run dev
```

Open http://127.0.0.1:5173. On Windows PowerShell, use `npm.cmd run dev` if execution policy blocks `npm.ps1`.

```sh
npm test
npm run build
npm run preview
```

`npm run build` creates the deployable `dist` folder. The preview command serves this production build. The included server is for local development, not production hosting.

## Content and booking

- `index.html`: visible copy, service cards, weekly hours, gallery, metadata, and business structured data.
- `styles.css`: responsive layout, colours, and typography.
- `salon.js`: contact details, WhatsApp messages, and Malaysia-time opening status.
- `app.js`: menu, booking dialog, gallery filters, lightbox, and live opening status.
- `public/images`: locally hosted inspiration photographs.
- `public/fonts`: locally hosted Cormorant Garamond and DM Sans fonts. Licenses are in `licenses` and included in the production build.

Booking buttons open a form, then send the visitor to WhatsApp with a prepared message. The visitor must send the message themselves. There is no automatic confirmation, payment, database, analytics, or live appointment availability. Name and preferred date are optional, and the date cannot be in the past in Malaysia time. Without JavaScript, booking links go directly to WhatsApp.

If changing the telephone, address, services or schedule, update both `salon.js` and the relevant HTML, including JSON-LD structured data. Hours from the supplied screenshot are Monday 11 am–5 pm and Tuesday–Sunday 10 am–7 pm. Holiday hours may differ.

## Publish

1. Run `npm run build` and preview the result with `npm run preview`.
2. In your Cloudflare account, create a Pages project and upload the contents of `dist` using Direct Upload. Alternatively connect this repository, use `npm run build` as the build command and `dist` as the output directory. Choose your preferred deployment method at project creation.
3. Cloudflare provides a public `pages.dev` address. A purchased domain can be connected later through the project’s custom-domain settings.
4. Once a domain is chosen, add an absolute canonical URL and Open Graph image URL to `index.html`, create a sitemap for that domain, and add its sitemap URL to `public/robots.txt`.
5. Add the final website link to the salon’s Google Business Profile and Instagram.

Hosting accounts, domain purchases, Git pushes, and live publication have not been performed by the project scripts.

## Owner review before launch

- Confirm phone **018-328 2070**, address, and Instagram account against the owner’s current details.
- Confirm the draft service categories (hair/styling, nails, makeup and bridal beauty). Exact treatments, prices, staff credentials, customer reviews and awards have deliberately not been invented.
- Replace stock inspiration images with the salon’s approved photographs if desired. The current gallery and hero are labelled as inspiration, not as the salon’s portfolio. No stock portrait is presented as Shalu.
- Check the website in a real desktop and mobile browser, including the menu, gallery filters, dialog closing, keyboard navigation and WhatsApp handoff. Browser automation was unavailable in the development session; the Node tests cover booking URLs, Malaysia-time boundaries, local assets, page references and structured data.

## Image sources

Stock photographs downloaded from Unsplash for the inspiration moodboard:

- `hero.jpg`: image identifier `photo-1519699047748-de8e457a634e`
- `hair.jpg`: image identifier `photo-1522337360788-8b13dee7a37e`
- `nails.jpg`: image identifier `photo-1604654894610-df63bc536371`
- `makeup.jpg`: image identifier `photo-1524504388940-b1c1722653e1`

Original image URLs use `https://images.unsplash.com/` followed by the image identifier. See https://unsplash.com/license for usage terms. Fonts are distributed under the SIL Open Font License; see the bundled license files.

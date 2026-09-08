import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile, access } from 'node:fs/promises';
import { salon, whatsappUrl, localDate, openingStatus } from '../salon.js';

test('appointment requests use the confirmed phone number and preserve special characters', () => {
  const url = new URL(whatsappUrl('Hair & styling', '2026-09-15', '  Aisha & Mei  '));
  assert.equal(url.origin, 'https://wa.me');
  assert.equal(url.pathname, '/60183282070');
  const message = url.searchParams.get('text');
  assert.ok(message.includes("I'm Aisha & Mei."));
  assert.ok(message.includes('hair & styling'));
  assert.ok(message.includes('15/09/2026'));
  assert.ok(message.includes('availability and pricing'));
  assert.equal([...url.searchParams].length, 1);
});

test('optional booking fields produce a complete request without empty details', () => {
  const message = new URL(whatsappUrl('', '', '   ')).searchParams.get('text');
  assert.equal(message, "Hi Shalu! I'd like to request an appointment.\nCould you please share your availability and pricing? Thank you!");
});

test('Monday uses the shorter 11 am to 5 pm schedule in Malaysia', () => {
  assert.deepEqual(openingStatus(new Date('2026-09-07T02:59:00Z')), { isOpen: false, hours: '11 am–5 pm' });
  assert.equal(openingStatus(new Date('2026-09-07T03:00:00Z')).isOpen, true);
  assert.equal(openingStatus(new Date('2026-09-07T08:59:00Z')).isOpen, true);
  assert.equal(openingStatus(new Date('2026-09-07T09:00:00Z')).isOpen, false);
});

test('Tuesday through Sunday use 10 am to 7 pm, with exact opening and closing boundaries', () => {
  for (let day = 8; day <= 13; day++) {
    const date = `2026-09-${String(day).padStart(2, '0')}`;
    assert.deepEqual(openingStatus(new Date(`${date}T01:59:00Z`)), { isOpen: false, hours: '10 am–7 pm' });
    assert.equal(openingStatus(new Date(`${date}T02:00:00Z`)).isOpen, true);
    assert.equal(openingStatus(new Date(`${date}T10:59:00Z`)).isOpen, true);
    assert.equal(openingStatus(new Date(`${date}T11:00:00Z`)).isOpen, false);
  }
});

test('booking dates and weekday changes follow Malaysia time, regardless of the visitor timezone', () => {
  assert.equal(localDate(new Date('2026-09-06T16:30:00Z')), '2026-09-07');
  assert.deepEqual(openingStatus(new Date('2026-09-06T16:30:00Z')), { isOpen: false, hours: '11 am–5 pm' });
});

const html = await readFile(new URL('../index.html', import.meta.url), 'utf8');

test('all in-page navigation targets exist and IDs are unique', () => {
  const ids = [...html.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]);
  assert.equal(new Set(ids).size, ids.length);
  for (const [, target] of html.matchAll(/href="#([^"]+)"/g)) assert.ok(ids.includes(target), `Missing target: ${target}`);
  for (const [, target] of html.matchAll(/(?:aria-labelledby|aria-controls|for)="([^"]+)"/g)) assert.ok(ids.includes(target), `Missing accessible reference: ${target}`);
});

test('all local images, fonts, scripts and styles are available', async () => {
  const css = await readFile(new URL('../styles.css', import.meta.url), 'utf8');
  const paths = new Set([...html.matchAll(/(?:src|href|data-image)="(\/[^"#]+)"/g)].map(match => match[1]));
  for (const [, asset] of css.matchAll(/url\(['"]?(\/[^)'" ]+)['"]?\)/g)) paths.add(asset);
  for (const asset of paths) {
    const prefix = /^\/(images|fonts)\//.test(asset) || asset === '/favicon.svg' ? '../public' : '..';
    await access(new URL(`${prefix}${asset}`, import.meta.url));
  }
});

test('business structured data matches the supplied phone and opening hours', () => {
  const json = html.match(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/)[1];
  const data = JSON.parse(json);
  assert.equal(data.telephone, `+${salon.phone}`);
  assert.equal(data.address.postalCode, '51200');
  assert.equal(data.openingHoursSpecification[0].opens, '10:00');
  assert.equal(data.openingHoursSpecification[0].closes, '19:00');
  assert.equal(data.openingHoursSpecification[1].dayOfWeek, 'Monday');
  assert.equal(data.openingHoursSpecification[1].opens, '11:00');
  assert.equal(data.openingHoursSpecification[1].closes, '17:00');
});

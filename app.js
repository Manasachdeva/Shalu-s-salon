import { salon, whatsappUrl, localDate, openingStatus } from './salon.js';

const bookingDialog = document.querySelector('#booking-dialog');
const serviceSelect = document.querySelector('#booking-service');
const dateInput = document.querySelector('#booking-date');
const nameInput = document.querySelector('#booking-name');
const mobileMenu = document.querySelector('#mobile-menu');
const menuToggle = document.querySelector('.menu-toggle');

function setMenu(open) {
  menuToggle.setAttribute('aria-expanded', String(open));
  menuToggle.setAttribute('aria-label', open ? 'Close menu' : 'Open menu');
  mobileMenu.hidden = !open;
}
menuToggle.addEventListener('click', () => setMenu(mobileMenu.hidden));
mobileMenu.addEventListener('click', (event) => { if (event.target.closest('a')) setMenu(false); });
document.addEventListener('keydown', (event) => { if (event.key === 'Escape' && !mobileMenu.hidden) { setMenu(false); menuToggle.focus(); } });
document.addEventListener('click', (event) => { if (!event.target.closest('.site-header') && !mobileMenu.hidden) setMenu(false); });
matchMedia('(min-width: 901px)').addEventListener('change', (event) => { if (event.matches) setMenu(false); });

function showDialog(dialog) {
  document.querySelectorAll('dialog[open]').forEach(openDialog => openDialog.close());
  dialog.showModal();
  document.body.classList.add('dialog-open');
}
document.querySelectorAll('dialog').forEach(dialog => {
  dialog.querySelector('[data-close]').addEventListener('click', () => dialog.close());
  dialog.addEventListener('click', (event) => {
    const bounds = dialog.getBoundingClientRect();
    if (event.target === dialog && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) dialog.close();
  });
  dialog.addEventListener('close', () => {
    if (!document.querySelector('dialog[open]')) document.body.classList.remove('dialog-open');
  });
});

document.querySelectorAll('[data-book]').forEach(link => {
  // Keep usable links available even without JavaScript or dialog support.
  link.href = whatsappUrl(link.dataset.service || '');
  link.addEventListener('click', event => {
    if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || typeof bookingDialog.showModal !== 'function') return;
    event.preventDefault();
    serviceSelect.value = link.dataset.service || 'Help me choose';
    dateInput.min = localDate();
    showDialog(bookingDialog);
  });
});

document.querySelector('#booking-form').addEventListener('submit', event => {
  event.preventDefault();
  dateInput.min = localDate();
  if (!event.currentTarget.reportValidity()) return;
  // User deliberately continues to WhatsApp; this website sends no message itself.
  window.location.assign(whatsappUrl(serviceSelect.value, dateInput.value, nameInput.value));
});

document.querySelectorAll('.gallery-filter').forEach(button => {
  button.addEventListener('click', () => {
    document.querySelectorAll('.gallery-filter').forEach(filter => {
      filter.classList.toggle('active', filter === button);
      filter.setAttribute('aria-pressed', String(filter === button));
    });
    let count = 0;
    document.querySelectorAll('.gallery-item').forEach(item => {
      item.hidden = button.dataset.filter !== 'all' && item.dataset.category !== button.dataset.filter;
      if (!item.hidden) count++;
    });
    document.querySelector('#gallery-status').textContent = `${count} inspiration ${count === 1 ? 'image' : 'images'} shown.`;
  });
});

const lightbox = document.querySelector('#lightbox');
document.querySelectorAll('.gallery-item').forEach(item => {
  item.addEventListener('click', () => {
    const image = document.querySelector('#lightbox-image');
    image.src = item.dataset.image;
    image.alt = item.querySelector('img').alt;
    document.querySelector('#lightbox-title').textContent = item.dataset.title;
    const bookingLink = lightbox.querySelector('[data-book]');
    bookingLink.dataset.service = item.dataset.category === 'hair' ? salon.services[0] : item.dataset.category === 'nails' ? 'Nails' : 'Makeup';
    bookingLink.href = whatsappUrl(bookingLink.dataset.service);
    showDialog(lightbox);
  });
});
document.querySelector('.privacy-trigger').addEventListener('click', () => showDialog(document.querySelector('#privacy-dialog')));

function updateHours() {
  const status = openingStatus();
  const element = document.querySelector('#open-status');
  element.textContent = `${status.isOpen ? 'Open now' : 'Currently closed'} · Today ${status.hours}`;
  element.classList.toggle('is-open', status.isOpen);
  const today = new Intl.DateTimeFormat('en-GB', { timeZone: salon.timeZone, weekday: 'short' }).format(new Date());
  document.querySelectorAll('[data-day]').forEach(row => row.classList.toggle('today', row.dataset.day === today));
}
updateHours();
setInterval(updateHours, 60_000);
document.querySelector('#year').textContent = new Date().getFullYear();

const observer = new IntersectionObserver(entries => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      document.querySelectorAll('.desktop-nav a').forEach(link => {
        const active = link.hash === `#${entry.target.id}`;
        if (active) link.setAttribute('aria-current', 'location');
        else link.removeAttribute('aria-current');
      });
    }
  });
}, { rootMargin: '-15% 0px -55% 0px', threshold: 0 });
document.querySelectorAll('main section[id]').forEach(section => observer.observe(section));

// Edit the salon's contact details and booking options here.
export const salon = Object.freeze({
  name: "Shalu's Salon",
  phone: '60183282070',
  phoneDisplay: '+60 18-328 2070',
  timeZone: 'Asia/Kuala_Lumpur',
  instagram: 'https://www.instagram.com/shalu_style_studio/',
  maps: "https://www.google.com/maps/search/?api=1&query=Shalu%27s+Salon+The+Richmond+Jalan+Kiara+3+Mont+Kiara+Kuala+Lumpur",
  services: ['Hair & styling', 'Nails', 'Makeup', 'Bridal beauty', 'Help me choose'],
});

export function whatsappUrl(service = '', date = '', name = '') {
  const cleanName = name.trim();
  const lines = [`Hi Shalu! ${cleanName ? `I'm ${cleanName}. ` : ''}I'd like to request an appointment${service ? ` for ${service.toLowerCase()}` : ''}.`];
  if (date) {
    const [year, month, day] = date.split('-');
    lines.push(`My preferred date is ${day}/${month}/${year}.`);
  }
  lines.push('Could you please share your availability and pricing? Thank you!');
  return `https://wa.me/${salon.phone}?text=${encodeURIComponent(lines.join('\n'))}`;
}

export function localDate(now = new Date()) {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: salon.timeZone, year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(now);
  const value = (type) => parts.find(part => part.type === type).value;
  return `${value('year')}-${value('month')}-${value('day')}`;
}

export function openingStatus(now = new Date()) {
  const parts = new Intl.DateTimeFormat('en-GB', { timeZone: salon.timeZone, weekday: 'short', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(now);
  const value = (type) => parts.find(part => part.type === type).value;
  const monday = value('weekday') === 'Mon';
  const minutes = Number(value('hour')) * 60 + Number(value('minute'));
  const open = monday ? 660 : 600;
  const close = monday ? 1020 : 1140;
  return { isOpen: minutes >= open && minutes < close, hours: monday ? '11 am–5 pm' : '10 am–7 pm' };
}

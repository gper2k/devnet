// DevKit shared JS: copyCode, dropdown/select helpers, group/toggle handlers
function copyCode(btn) {
  const code = btn.closest('.blockCode').querySelector('code');
  if (!code) return;
  navigator.clipboard.writeText(code.innerText).then(() => {
    const old = btn.textContent;
    btn.textContent = '✓ 완료';
    setTimeout(() => btn.textContent = old || '복사', 1500);
  });
}

// data-group: single select within group
document.addEventListener('click', e => {
  const btn = e.target.closest('[data-group]');
  if (btn) {
    const group = btn.dataset.group;
    document.querySelectorAll(`[data-group="${group}"]`).forEach(el => el.classList.remove('active'));
    btn.classList.add('active');
  }
});

// data-action="toggle": toggle active (unless part of a group)
document.addEventListener('click', e => {
  const btn = e.target.closest('[data-action="toggle"]');
  if (!btn || btn.dataset.group) return;
  btn.classList.toggle('active');
});

// dropDown open/close
document.addEventListener('click', e => {
  const dd = e.target.closest('.dropDown');
  if (!dd) {
    document.querySelectorAll('.dropDown.open').forEach(el => el.classList.remove('open'));
    return;
  }
  const isOpen = dd.classList.contains('open');
  document.querySelectorAll('.dropDown.open').forEach(el => el !== dd && el.classList.remove('open'));
  dd.classList.toggle('open', !isOpen);
});

// selectBox: update header text on item click
document.addEventListener('click', e => {
  const li = e.target.closest('.selectBox .dropBox li');
  if (!li || li.classList.contains('divider')) return;
  const dd = li.closest('.dropDown');
  const header = dd.querySelector('header');
  const icon = header ? header.querySelector('i') : null;
  const span = header ? header.querySelector('span') : null;
  if (span) span.textContent = li.querySelector('span')?.textContent ?? li.textContent;
  dd.querySelectorAll('.dropBox li').forEach(el => el.classList.remove('active'));
  li.classList.add('active');
  dd.classList.remove('open');
});

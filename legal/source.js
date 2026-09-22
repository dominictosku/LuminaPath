fetch('source.json')
  .then(response => {
    if (!response.ok) throw new Error('Source metadata unavailable');
    return response.json();
  })
  .then(info => {
    const source = new URL(info.url);
    if (source.protocol !== 'https:' && source.protocol !== 'http:') return;
    document.getElementById('source').href = source.href;
    if (info.revision) {
      document.getElementById('version').textContent = `Source revision: ${info.revision}`;
    }
    document.getElementById('web-licenses').hidden = !info.web;
  })
  .catch(() => { /* Keep the development source link when metadata is unavailable. */ });

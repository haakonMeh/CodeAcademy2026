/* Felles effekter for presentasjonene i samling 5 */
(function () {
  const deck = window.Reveal;

  deck.initialize({
    hash: true,
    controls: true,
    progress: true,
    slideNumber: 'c/t',
    center: true,
    width: 1280,
    height: 760,
    margin: 0.06,
    transition: 'slide',
    backgroundTransition: 'fade',
    autoAnimateDuration: 0.8,
    plugins: [RevealNotes, RevealHighlight, RevealZoom],
  });

  /* ---------- Partikkelnettverk på tittel-/kapittelslides ---------- */
  function particles(canvas) {
    const ctx = canvas.getContext('2d');
    let w, h, pts, raf;
    const colors = ['#f07d00', '#cf022b', '#8b1d82', '#ffffff'];
    function resize() {
      w = canvas.width = canvas.offsetWidth * devicePixelRatio;
      h = canvas.height = canvas.offsetHeight * devicePixelRatio;
      pts = Array.from({ length: 70 }, () => ({
        x: Math.random() * w, y: Math.random() * h,
        vx: (Math.random() - .5) * .6 * devicePixelRatio, vy: (Math.random() - .5) * .6 * devicePixelRatio,
        r: (Math.random() * 2 + 1) * devicePixelRatio, c: colors[Math.floor(Math.random() * colors.length)],
      }));
    }
    function tick() {
      ctx.clearRect(0, 0, w, h);
      const max = 140 * devicePixelRatio;
      for (const p of pts) {
        p.x += p.vx; p.y += p.vy;
        if (p.x < 0 || p.x > w) p.vx *= -1;
        if (p.y < 0 || p.y > h) p.vy *= -1;
      }
      for (let i = 0; i < pts.length; i++) {
        for (let j = i + 1; j < pts.length; j++) {
          const a = pts[i], b = pts[j], d = Math.hypot(a.x - b.x, a.y - b.y);
          if (d < max) {
            ctx.strokeStyle = `rgba(255,255,255,${0.18 * (1 - d / max)})`;
            ctx.lineWidth = devicePixelRatio;
            ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
          }
        }
      }
      for (const p of pts) { ctx.fillStyle = p.c; ctx.beginPath(); ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2); ctx.fill(); }
      raf = requestAnimationFrame(tick);
    }
    return {
      start() { resize(); cancelAnimationFrame(raf); tick(); },
      stop() { cancelAnimationFrame(raf); },
    };
  }

  /* ---------- Skrivemaskin-terminal ---------- */
  // Linjeprefiks: "$ " kommando (skrives tegn for tegn), "! " feil, "+ " ok, "# " dimmet, ellers vanlig output
  const esc = (s) => s.replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));
  function typewriter(term) {
    const out = term.querySelector('pre.tw');
    const src = term.querySelector('script.tw-src').textContent.replace(/^\n/, '').replace(/\s+$/, '').split('\n');
    let token = 0;
    async function run() {
      const my = ++token;
      out.innerHTML = '';
      term.classList.remove('blocked-flash');
      const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
      await sleep(500);
      for (const raw of src) {
        if (my !== token) return;
        const kind = raw.slice(0, 2);
        const text = ['$ ', '! ', '+ ', '# '].includes(kind) ? raw.slice(2) : raw;
        const line = document.createElement('div');
        out.appendChild(line);
        if (kind === '$ ') {
          line.innerHTML = '<span class="prompt">❯ </span><span class="cmd"></span><span class="cursor">&nbsp;</span>';
          const cmd = line.querySelector('.cmd');
          for (const ch of text) {
            if (my !== token) return;
            cmd.textContent += ch;
            await sleep(28 + Math.random() * 40);
          }
          line.querySelector('.cursor').remove();
          await sleep(450);
        } else {
          const cls = { '! ': 'err', '+ ': 'ok', '# ': 'dim' }[kind] || '';
          line.innerHTML = `<span class="${cls}">${esc(text) || '&nbsp;'}</span>`;
          if (kind === '! ' && term.dataset.flash !== undefined) term.classList.add('blocked-flash');
          await sleep(text.startsWith('…') ? 900 : 160);
        }
      }
      const end = document.createElement('div');
      end.innerHTML = '<span class="prompt">❯ </span><span class="cursor">&nbsp;</span>';
      out.appendChild(end);
    }
    return { start: run, stop() { token++; } };
  }

  /* ---------- Nedtelling for workshop ---------- */
  function timer(el) {
    const total = Number(el.dataset.minutes || 60) * 60;
    let left = total, iv = null;
    const disp = el.querySelector('.timer-big'), bar = el.querySelector('.timer-progress span');
    const render = () => {
      const m = Math.floor(left / 60), s = left % 60;
      disp.textContent = `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
      bar.style.width = `${100 * (1 - left / total)}%`;
    };
    el.querySelector('[data-act=start]').onclick = () => {
      if (iv) { clearInterval(iv); iv = null; return; }
      iv = setInterval(() => { left = Math.max(0, left - 1); render(); if (!left) clearInterval(iv); }, 1000);
    };
    el.querySelector('[data-act=reset]').onclick = () => { clearInterval(iv); iv = null; left = total; render(); };
    render();
  }

  /* ---------- Oppsett ---------- */
  let particleFx = null;
  const terms = new Map();

  deck.on('ready', () => {
    const c = document.createElement('canvas');
    c.id = 'particles';
    document.querySelector('.reveal').insertBefore(c, document.querySelector('.reveal .slides'));
    particleFx = particles(c);
    document.querySelectorAll('.terminal').forEach((t) => terms.set(t, typewriter(t)));
    document.querySelectorAll('.timer').forEach(timer);
    document.querySelectorAll('.quiz .q').forEach((q) => q.addEventListener('click', () => q.classList.toggle('revealed')));
    onSlide(deck.getCurrentSlide());
  });

  const logo = document.querySelector('.deck-logo');
  const tag = document.querySelector('.deck-tag');

  function onSlide(slide) {
    const showParticles = slide.hasAttribute('data-particles');
    document.getElementById('particles')?.classList.toggle('on', showParticles);
    if (particleFx) showParticles ? particleFx.start() : particleFx.stop();
    terms.forEach((t, el) => (slide.contains(el) && !el.matches('.fragment:not(.visible)') ? t.start() : t.stop()));
    const dark = slide.classList.contains('dark');
    if (logo) {
      logo.src = dark ? logo.dataset.white : logo.dataset.color;
      logo.classList.toggle('hide', slide.classList.contains('no-logo'));
    }
    if (tag) tag.classList.toggle('light', dark);
    syncFragments(slide);
  }

  // Fragmenter med data-activate="<selector>" markerer et element som aktivt (f.eks. i agent-loopen)
  // Fragmenter med data-addclass="<selector>|<class>" legger til en klasse mens de er synlige
  function syncFragments(slide) {
    slide.querySelectorAll('[data-activate]').forEach((f) => {
      const t = slide.querySelector(f.dataset.activate);
      if (t) t.classList.remove('active');
    });
    const visible = [...slide.querySelectorAll('.fragment.visible[data-activate]')];
    const last = visible[visible.length - 1];
    if (last) slide.querySelector(last.dataset.activate)?.classList.add('active');

    slide.querySelectorAll('[data-addclass]').forEach((f) => {
      const [sel, cls] = f.dataset.addclass.split('|');
      slide.querySelectorAll(sel).forEach((t) => t.classList.toggle(cls, f.classList.contains('visible')));
    });
  }

  deck.on('slidechanged', (e) => onSlide(e.currentSlide));
  deck.on('fragmentshown', (e) => {
    e.fragments.forEach((f) => terms.get(f)?.start());
    syncFragments(deck.getCurrentSlide());
  });
  deck.on('fragmenthidden', () => syncFragments(deck.getCurrentSlide()));

  // Tast "T" restarter terminal-animasjonen på gjeldende slide
  deck.addKeyBinding({ keyCode: 84, key: 'T', description: 'Spill av terminal på nytt' }, () => {
    terms.forEach((t, el) => deck.getCurrentSlide().contains(el) && t.start());
  });
})();

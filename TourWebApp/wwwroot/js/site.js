/* ============================
   REVEAL EFFECT
============================ */
(() => {
    const els = document.querySelectorAll('.reveal');
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reduceMotion) {
        els.forEach(el => el.classList.add('show'));
        return;
    }

    if (!('IntersectionObserver' in window)) {
        els.forEach(el => el.classList.add('show'));
        return;
    }

    const io = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.classList.add('show');
                io.unobserve(e.target);
            }
        });
    }, { threshold: 0.15, rootMargin: '0px 0px -8% 0px' });

    els.forEach(el => io.observe(el));
})();

/* ============================
   NAVBAR STICKY (fix class name)
============================ */
(() => {
    const nav = document.querySelector('.navbar-main');  // 🔥 FIXED: đúng class mới
    if (!nav) return;

    const toggle = () => {
        nav.classList.toggle('stuck', window.scrollY > 8);
    };

    toggle();
    window.addEventListener('scroll', toggle);
})();

/* ============================
   HERO TEXT REPLAY ANIMATION
============================ */
(() => {
    const carousel = document.querySelector('#heroCarousel');
    const heroText = carousel?.querySelector('.hero-enter');
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (!carousel || !heroText || reduceMotion) return;

    const replay = () => {
        heroText.classList.remove('is-in');
        window.requestAnimationFrame(() => {
            window.requestAnimationFrame(() => heroText.classList.add('is-in'));
        });
    };

    replay();
    carousel.addEventListener('slide.bs.carousel', replay);
})();

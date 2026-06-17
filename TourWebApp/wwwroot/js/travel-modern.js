(() => {
    const revealElements = document.querySelectorAll('.reveal');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reducedMotion || !('IntersectionObserver' in window)) {
        revealElements.forEach((el) => el.classList.add('show'));
        return;
    }

    const observer = new IntersectionObserver((entries, obs) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                entry.target.classList.add('show');
                obs.unobserve(entry.target);
            }
        });
    }, { threshold: 0.16, rootMargin: '0px 0px -10% 0px' });

    revealElements.forEach((el) => observer.observe(el));
})();

(() => {
    const toast = document.querySelector('.app-toast');
    if (!toast) return;

    const close = () => {
        if (toast.classList.contains('is-hiding')) return;
        toast.classList.add('is-hiding');
        window.setTimeout(() => toast.remove(), 320);
    };

    toast.querySelector('.app-toast-close')?.addEventListener('click', close);
    window.setTimeout(close, 4200);
})();

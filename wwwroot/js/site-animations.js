(function () {
    'use strict';

    function initReveal() {
        var items = document.querySelectorAll('.reveal');
        if (!items.length) return;
        if (!('IntersectionObserver' in window)) {
            items.forEach(function (el) { el.classList.add('in-view'); });
            return;
        }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('in-view');
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15 });
        items.forEach(function (el) { io.observe(el); });
    }

    // Animates an element's integer text content from 0 up to its current value.
    function countUp(el, durationMs) {
        var target = parseInt((el.textContent || '0').replace(/[^0-9-]/g, ''), 10);
        if (isNaN(target)) return;
        var start = performance.now();
        var duration = durationMs || 800;
        function tick(now) {
            var progress = Math.min(1, (now - start) / duration);
            var eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = Math.round(target * eased).toString();
            if (progress < 1) requestAnimationFrame(tick);
            else el.textContent = target.toString();
        }
        requestAnimationFrame(tick);
    }

    function initCountUp() {
        document.querySelectorAll('.count-up').forEach(function (el) { countUp(el); });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initReveal();
        initCountUp();
    });

    window.stsAnimate = { countUp: countUp };
})();

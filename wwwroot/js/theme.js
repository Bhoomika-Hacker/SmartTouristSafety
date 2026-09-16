(function () {
    'use strict';

    var STORAGE_KEY = 'sts-theme';

    function getStoredTheme() {
        try { return localStorage.getItem(STORAGE_KEY); } catch (e) { return null; }
    }

    function preferredTheme() {
        var stored = getStoredTheme();
        if (stored === 'light' || stored === 'dark') return stored;
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        window.dispatchEvent(new CustomEvent('sts:theme-changed', { detail: { theme: theme } }));
    }

    function setTheme(theme) {
        try { localStorage.setItem(STORAGE_KEY, theme); } catch (e) { /* ignore */ }
        applyTheme(theme);
        updateToggleUi(theme);
    }

    function updateToggleUi(theme) {
        var btn = document.getElementById('themeToggle');
        if (!btn) return;
        btn.setAttribute('aria-checked', theme === 'dark' ? 'true' : 'false');
        btn.title = theme === 'dark' ? 'Switch to day mode' : 'Switch to night mode';
        var icon = btn.querySelector('i');
        if (icon) icon.className = theme === 'dark' ? 'bi bi-moon-stars-fill' : 'bi bi-sun-fill';
    }

    // Applied immediately (this file loads in <head> before body paints) to avoid a flash.
    applyTheme(preferredTheme());

    document.addEventListener('DOMContentLoaded', function () {
        updateToggleUi(document.documentElement.getAttribute('data-bs-theme'));
        var btn = document.getElementById('themeToggle');
        if (btn) {
            btn.addEventListener('click', function () {
                var current = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
                setTheme(current === 'dark' ? 'light' : 'dark');
            });
        }
    });

    window.stsTheme = { get: function () { return document.documentElement.getAttribute('data-bs-theme') || 'light'; }, set: setTheme };
})();

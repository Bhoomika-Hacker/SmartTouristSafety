(function () {
    'use strict';

    // Free, keyless CARTO basemap tiles — light and dark variants so the map
    // actually follows the site's day/night theme instead of staying static.
    var TILE_URLS = {
        light: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
        dark: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
    };
    var TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>';

    var RISK_COLORS = {
        Safe: '#1a9c5c',
        Caution: '#e0a800',
        HighRisk: '#e0601c',
        Restricted: '#c0392b'
    };

    function riskColor(level) {
        return RISK_COLORS[level] || '#6c757d';
    }

    // Creates a Leaflet map in `elId`, wires it to swap tile layers whenever the
    // site theme toggles, and returns { map, setView }.
    function createMap(elId, opts) {
        if (!window.L) return null;
        var el = document.getElementById(elId);
        if (!el) return null;

        var theme = (window.stsTheme && window.stsTheme.get()) || 'light';
        var map = L.map(elId, Object.assign({ zoomControl: true, attributionControl: true }, opts || {}));

        var tileLayer = L.tileLayer(TILE_URLS[theme], { attribution: TILE_ATTRIBUTION, maxZoom: 19, subdomains: 'abcd' }).addTo(map);

        window.addEventListener('sts:theme-changed', function (e) {
            var next = e.detail && e.detail.theme === 'dark' ? 'dark' : 'light';
            map.removeLayer(tileLayer);
            tileLayer = L.tileLayer(TILE_URLS[next], { attribution: TILE_ATTRIBUTION, maxZoom: 19, subdomains: 'abcd' }).addTo(map);
        });

        return map;
    }

    function divIcon(html, size) {
        size = size || 22;
        return L.divIcon({
            html: html,
            className: '',
            iconSize: [size, size],
            iconAnchor: [size / 2, size / 2]
        });
    }

    function touristIcon() {
        return divIcon('<div style="width:16px;height:16px;border-radius:50%;background:#2563eb;border:3px solid #fff;box-shadow:0 0 0 3px rgba(37,99,235,0.35);"></div>');
    }

    function policeIcon() {
        return divIcon('<div class="pin-pulse" style="display:flex;align-items:center;justify-content:center;"></div>', 18);
    }

    window.stsMap = {
        createMap: createMap,
        riskColor: riskColor,
        touristIcon: touristIcon,
        policeIcon: policeIcon,
        divIcon: divIcon
    };
})();

(function () {
    let activeTilt = null;

    function resetTilt(el) {
        if (!el) return;
        el.style.setProperty('--rx', '0deg');
        el.style.setProperty('--ry', '0deg');
    }

    document.addEventListener('pointermove', function (e) {
        const target = e.target instanceof Element ? e.target : null;
        if (!target) return;

        const lightEl = target.closest('.tilt, .tilt-light');
        if (activeTilt && activeTilt !== lightEl) {
            resetTilt(activeTilt);
            activeTilt = null;
        }

        if (!lightEl) return;

        const rect = lightEl.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0) return;

        const x = (e.clientX - rect.left) / rect.width;
        const y = (e.clientY - rect.top) / rect.height;
        lightEl.style.setProperty('--mx', (x * 100).toFixed(1) + '%');
        lightEl.style.setProperty('--my', (y * 100).toFixed(1) + '%');

        if (lightEl.classList.contains('tilt')) {
            lightEl.style.setProperty('--rx', ((0.5 - y) * 7).toFixed(2) + 'deg');
            lightEl.style.setProperty('--ry', ((x - 0.5) * 8).toFixed(2) + 'deg');
            activeTilt = lightEl;
        }
    }, { passive: true });

    document.addEventListener('pointerout', function (e) {
        if (!activeTilt) return;
        const related = e.relatedTarget instanceof Element ? e.relatedTarget : null;
        if (!related || !activeTilt.contains(related)) {
            resetTilt(activeTilt);
            activeTilt = null;
        }
    }, { passive: true });

    let shellHelper = null;

    document.addEventListener('keydown', function (e) {
        if ((e.metaKey || e.ctrlKey) && e.key && e.key.toLowerCase() === 'k') {
            if (shellHelper) {
                e.preventDefault();
                shellHelper.invokeMethodAsync('OnOpenSearchShortcut');
            }
        }
    });

    // Restore saved theme customization if available
    try {
        const savedTheme = localStorage.getItem('luminapath-theme');
        if (savedTheme && savedTheme !== 'default') {
            document.documentElement.setAttribute('data-theme', savedTheme);
        } else {
            document.documentElement.removeAttribute('data-theme');
        }
    } catch (_) {}

    // Always keep motion effects enabled
    try {
        document.body.classList.remove('no-motion');
    } catch (_) {}

    window.luminaUi = {
        registerShell: function (dotNetHelper) {
            shellHelper = dotNetHelper;
            return false;
        },
        unregisterShell: function () {
            shellHelper = null;
        },
        toggleMotion: function () {
            return false;
        },
        setMotionDisabled: function () {
            return false;
        },
        applyTheme: function (themeName) {
            const resolved = (!themeName || themeName === 'default' || themeName === 'mint') ? 'default' : themeName;
            if (resolved === 'default') {
                document.documentElement.removeAttribute('data-theme');
                try { localStorage.setItem('luminapath-theme', 'default'); } catch (_) {}
            } else {
                document.documentElement.setAttribute('data-theme', resolved);
                try { localStorage.setItem('luminapath-theme', resolved); } catch (_) {}
            }
            try {
                window.dispatchEvent(new CustomEvent('luminapath-theme-changed', { detail: resolved }));
            } catch (_) {}
            return resolved;
        },
        getTheme: function () {
            try {
                const stored = localStorage.getItem('luminapath-theme');
                if (stored) return stored;
            } catch (_) {}
            return document.documentElement.getAttribute('data-theme') || 'default';
        }
    };

    window.downloadFileFromStream = window.downloadFileFromStream || async function (fileName, contentStreamReference) {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    };
})();

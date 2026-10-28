(function () {
    const reducedMotionQuery = window.matchMedia
        ? window.matchMedia('(prefers-reduced-motion: reduce)')
        : { matches: false };

    function isMotionDisabled() {
        return reducedMotionQuery.matches || document.body.classList.contains('no-motion');
    }

    let activeTilt = null;

    function resetTilt(el) {
        if (!el) return;
        el.style.setProperty('--rx', '0deg');
        el.style.setProperty('--ry', '0deg');
    }

    document.addEventListener('pointermove', function (e) {
        if (isMotionDisabled()) {
            if (activeTilt) {
                resetTilt(activeTilt);
                activeTilt = null;
            }
            return;
        }

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

    window.luminaUi = {
        registerShell: function (dotNetHelper) {
            shellHelper = dotNetHelper;
            return isMotionDisabled();
        },
        unregisterShell: function () {
            shellHelper = null;
        },
        toggleMotion: function () {
            const disabled = document.body.classList.toggle('no-motion');
            if (disabled && activeTilt) {
                resetTilt(activeTilt);
                activeTilt = null;
            }
            return disabled;
        },
        setMotionDisabled: function (disabled) {
            document.body.classList.toggle('no-motion', !!disabled);
            if (disabled && activeTilt) {
                resetTilt(activeTilt);
                activeTilt = null;
            }
            return isMotionDisabled();
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

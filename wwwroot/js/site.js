// ── Theme toggle (light / dark) ──

(function () {
  function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('pw-theme', theme);
  }

  function toggleTheme() {
    var current = document.documentElement.getAttribute('data-theme') || 'light';
    applyTheme(current === 'dark' ? 'light' : 'dark');
  }

  document.addEventListener('DOMContentLoaded', function () {
    // Bind all toggle buttons (dashboard + auth pages)
    var toggles = document.querySelectorAll('.theme-toggle');
    toggles.forEach(function (btn) {
      btn.addEventListener('click', toggleTheme);
    });

    // ── Progress bar animation on load ──
    var bars = document.querySelectorAll('.progress-bar[data-width]');
    if (bars.length) {
      // Small delay so the CSS transition triggers (0 → target)
      requestAnimationFrame(function () {
        requestAnimationFrame(function () {
          bars.forEach(function (bar, i) {
            setTimeout(function () {
              bar.style.width = bar.getAttribute('data-width');
            }, i * 100);
          });
        });
      });
    }
  });
})();

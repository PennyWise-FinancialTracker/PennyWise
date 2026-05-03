// Theme toggle, progress animation, and chart interactions.

(function () {
  function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('pw-theme', theme);
  }

  function toggleTheme() {
    var current = document.documentElement.getAttribute('data-theme') || 'light';
    applyTheme(current === 'dark' ? 'light' : 'dark');
  }

  function bindThemeToggles() {
    var toggles = document.querySelectorAll('.theme-toggle');
    toggles.forEach(function (btn) {
      btn.addEventListener('click', toggleTheme);
    });
  }

  function animateProgressBars() {
    var bars = document.querySelectorAll('.progress-bar[data-width]');
    if (!bars.length) return;

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

  function bindChartTooltips() {
    var chartTargets = document.querySelectorAll('.chart-point, .donut-segment');
    if (!chartTargets.length) return;

    var tooltip = document.createElement('div');
    tooltip.className = 'chart-hover-tooltip';
    tooltip.setAttribute('role', 'status');
    tooltip.setAttribute('aria-live', 'polite');
    document.body.appendChild(tooltip);

    function showTooltip(target, clientX, clientY) {
      var title = target.getAttribute('data-chart-title') || '';
      var value = target.getAttribute('data-chart-value') || '';
      tooltip.innerHTML = '<strong>' + title + '</strong><span>' + value + '</span>';
      tooltip.classList.add('is-visible');
      moveTooltip(clientX, clientY);
    }

    function moveTooltip(clientX, clientY) {
      var offset = 14;
      var left = clientX + offset;
      var top = clientY + offset;
      var rect = tooltip.getBoundingClientRect();

      if (left + rect.width > window.innerWidth - 12) {
        left = clientX - rect.width - offset;
      }

      if (top + rect.height > window.innerHeight - 12) {
        top = clientY - rect.height - offset;
      }

      tooltip.style.left = Math.max(12, left) + 'px';
      tooltip.style.top = Math.max(12, top) + 'px';
    }

    function hideTooltip() {
      tooltip.classList.remove('is-visible');
    }

    chartTargets.forEach(function (target) {
      target.addEventListener('mouseenter', function (event) {
        showTooltip(target, event.clientX, event.clientY);
      });

      target.addEventListener('mousemove', function (event) {
        moveTooltip(event.clientX, event.clientY);
      });

      target.addEventListener('mouseleave', hideTooltip);

      target.addEventListener('focus', function () {
        var rect = target.getBoundingClientRect();
        showTooltip(target, rect.left + rect.width / 2, rect.top + rect.height / 2);
      });

      target.addEventListener('blur', hideTooltip);
    });
  }

  function bindTxManageToggles() {
    document.addEventListener('click', function (event) {
      var trigger = event.target.closest('[data-tx-toggle]');
      if (!trigger) return;
      event.preventDefault();
      var key = trigger.getAttribute('data-tx-toggle');
      var panel = document.querySelector('[data-tx-manage="' + key + '"]');
      if (!panel) return;
      var triggers = document.querySelectorAll('[data-tx-toggle="' + key + '"].manage-btn');
      var open = panel.hasAttribute('hidden');
      if (open) {
        panel.removeAttribute('hidden');
        triggers.forEach(function (t) { t.style.display = 'none'; });
      } else {
        panel.setAttribute('hidden', '');
        triggers.forEach(function (t) { t.style.display = ''; });
      }
    });
  }

  function bindFillSelects() {
    document.querySelectorAll('select[data-fills]').forEach(function (select) {
      select.addEventListener('change', function () {
        var target = document.querySelector(select.getAttribute('data-fills'));
        if (!target) return;
        var opt = select.options[select.selectedIndex];
        var fillValue = opt && opt.getAttribute('data-fill-value');
        if (!fillValue) return;
        var current = parseFloat(target.value);
        if (target.value === '' || isNaN(current) || current === 0) {
          target.value = fillValue;
        }
      });
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    bindThemeToggles();
    animateProgressBars();
    bindChartTooltips();
    bindFillSelects();
    bindTxManageToggles();
  });
})();

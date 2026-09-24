(function () {
  function setVisibility(field, visible) {
    var input = field.querySelector('[data-password-input]');
    var toggle = field.querySelector('[data-password-toggle]');
    var showIcon = toggle ? toggle.querySelector('[data-password-icon="show"]') : null;
    var hideIcon = toggle ? toggle.querySelector('[data-password-icon="hide"]') : null;

    if (!input || !toggle) {
      return;
    }

    input.type = visible ? 'text' : 'password';
    toggle.setAttribute('aria-pressed', String(visible));
    toggle.setAttribute('aria-label', visible ? 'Hide password' : 'Show password');

    if (showIcon) {
      showIcon.hidden = visible;
    }

    if (hideIcon) {
      hideIcon.hidden = !visible;
    }
  }

  document.addEventListener('click', function (event) {
    var toggle = event.target.closest('[data-password-toggle]');
    if (!toggle) {
      return;
    }

    var field = toggle.closest('.password-field');
    if (!field) {
      return;
    }

    var input = field.querySelector('[data-password-input]');
    if (!input) {
      return;
    }

    setVisibility(field, input.type === 'password');
  });

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.password-field').forEach(function (field) {
      setVisibility(field, false);
    });
  });
})();

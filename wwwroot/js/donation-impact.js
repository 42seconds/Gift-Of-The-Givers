// Live "Your impact" estimate on the donation form.
// Mirrors Services/DonationImpactCalculator.cs; the rates and item costs are rendered into the page by the server.
(function () {
  const card = document.querySelector('[data-impact-estimate]');
  if (!card) {
    return;
  }

  const config = JSON.parse(card.getAttribute('data-impact-config'));
  const amountInput = document.getElementById('Donation_Amount');
  const currencySelect = document.getElementById('Donation_Currency');
  const frequencySelect = document.getElementById('Donation_Frequency');
  const totalText = card.querySelector('[data-impact-total]');
  const list = card.querySelector('[data-impact-list]');

  // Enum select lists post numeric values, so map them back to their names.
  const currencyName = () => config.currencies[Number(currencySelect.value)] || 'ZAR';
  const isRecurring = () => config.frequencies[Number(frequencySelect.value)] === 'Recurring';

  function calculate(amount) {
    let remaining = amount * (config.ratesToZar[currencyName()] || 1) * (isRecurring() ? 12 : 1);
    const total = remaining;
    const lines = [];

    config.items.forEach((item, i) => {
      const isLast = i === config.items.length - 1;
      const budget = isLast ? remaining : Math.max(total / 2, item.costZar);
      const quantity = Math.floor(Math.min(budget, remaining) / item.costZar);
      if (quantity > 0) {
        lines.push({ quantity, label: quantity === 1 ? item.singular : item.plural });
        remaining -= quantity * item.costZar;
      }
    });

    return { total, lines };
  }

  function render() {
    const amount = parseFloat((amountInput.value || '').replace(',', '.'));
    list.innerHTML = '';

    if (!amount || amount <= 0) {
      totalText.textContent = 'Enter an amount to see what your gift can do.';
      return;
    }

    const result = calculate(amount);
    if (result.lines.length === 0) {
      totalText.textContent = 'Every rand counts. Your gift joins others to fund a hot meal.';
      return;
    }

    const period = isRecurring() ? 'Over a year, your monthly gift could provide:' : 'Your gift could provide:';
    totalText.textContent = period;

    result.lines.forEach(line => {
      const li = document.createElement('li');
      const strong = document.createElement('strong');
      strong.textContent = line.quantity.toLocaleString();
      li.append(strong, ' ' + line.label);
      list.appendChild(li);
    });
  }

  [amountInput, currencySelect, frequencySelect].forEach(el => {
    el.addEventListener('input', render);
    el.addEventListener('change', render);
  });

  render();
})();

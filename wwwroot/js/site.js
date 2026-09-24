(function () {
  const app = document.querySelector('[data-app]');
  if (!app) {
    return;
  }

  const pages = Array.from(document.querySelectorAll('.page-view'));
  const navButtons = Array.from(document.querySelectorAll('.nav-link, .mobile-nav-link'));
  const mobileMenu = document.querySelector('[data-mobile-menu]');
  const menuToggle = document.querySelector('[data-menu-toggle]');
  const toast = document.querySelector('[data-toast]');
  const toastMessage = document.querySelector('[data-toast-message]');
  const toastClose = document.querySelector('[data-toast-close]');
  const currentYear = document.querySelector('[data-current-year]');
  const scheduleModal = document.querySelector('[data-modal="schedule"]');
  const body = document.body;

  const currencyConfig = {
    ZAR: {
      symbol: 'R',
      presets: [250, 500, 1000],
      water: 'R500 / $30',
      medical: 'R1000 / $60',
    },
    USD: {
      symbol: '$',
      presets: [15, 30, 60],
      water: '$30 / R500',
      medical: '$60 / R1000',
    },
    EUR: {
      symbol: '€',
      presets: [15, 30, 60],
      water: '€30 / R550',
      medical: '€60 / R1100',
    },
  };

  const state = {
    page: 'home',
    initiativeCategory: 'All',
    initiativeSearch: '',
    dashboardTab: 'overview',
    donationFilter: 'all',
    dashboardSearch: '',
    currency: 'ZAR',
    frequency: 'one-time',
    selectedPreset: 500,
    customAmount: '',
    donationProject: 'General Relief Fund',
    donationTax: false,
    donationSuccess: null,
    volunteerSkills: ['Medical', 'Logistics'],
    volunteerSuccess: null,
    contactSuccess: null,
  };

  const initialDonations = [
    {
      id: 'don-001',
      donorName: 'Sarah Jenkins',
      amount: 500,
      currency: 'USD',
      project: 'Syria Relief',
      status: 'Confirmed',
      date: '2026-08-27T10:30:00Z',
      email: 'sarah.j@example.com',
      frequency: 'one-time',
    },
    {
      id: 'don-002',
      donorName: 'Tech Corp Ltd.',
      amount: 10000,
      currency: 'USD',
      project: 'General Fund',
      status: 'Pending',
      date: '2026-08-27T09:15:00Z',
      email: 'giving@techcorp.io',
      frequency: 'one-time',
    },
    {
      id: 'don-003',
      donorName: "Michael O'Connor",
      amount: 150,
      currency: 'USD',
      project: 'Yemen Aid',
      status: 'Confirmed',
      date: '2026-08-26T16:45:00Z',
      email: 'michael.oc@example.com',
      frequency: 'monthly',
    },
    {
      id: 'don-004',
      donorName: 'Amina Patel',
      amount: 2500,
      currency: 'USD',
      project: 'Palestine Appeal',
      status: 'Confirmed',
      date: '2026-08-26T14:20:00Z',
      email: 'amina.p@example.com',
      frequency: 'one-time',
    },
    {
      id: 'don-005',
      donorName: 'Thabo Mokoena',
      amount: 750,
      currency: 'ZAR',
      project: 'KwaZulu-Natal Flood Relief',
      status: 'Confirmed',
      date: '2026-08-25T11:10:00Z',
      email: 'thabo.m@domain.co.za',
      frequency: 'monthly',
    },
    {
      id: 'don-006',
      donorName: 'Elena Rostova',
      amount: 300,
      currency: 'EUR',
      project: 'Clean Water Boreholes',
      status: 'Confirmed',
      date: '2026-08-25T08:05:00Z',
      email: 'elena.rostova@eu-aid.org',
      frequency: 'one-time',
    },
  ];

  const initialVolunteers = [
    {
      id: 'vol-001',
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      phone: '+27 82 555 0192',
      availability: 'Immediate Deployment',
      skills: ['Medical', 'Logistics', 'First Aid'],
      dateRegistered: '2026-08-26',
      status: 'In Field',
    },
    {
      id: 'vol-002',
      fullName: 'Dr. Zayd Osman',
      email: 'zayd.osman@medic.org.za',
      phone: '+27 83 400 9182',
      availability: 'Immediate Deployment',
      skills: ['Trauma Surgery', 'Medical', 'Search & Rescue'],
      dateRegistered: '2026-08-24',
      status: 'In Field',
    },
    {
      id: 'vol-003',
      fullName: 'Nomvula Dlamini',
      email: 'nomvula.d@transport.co.za',
      phone: '+27 71 332 8901',
      availability: 'Weekends Only',
      skills: ['Logistics', 'Driving', 'Heavy Vehicles'],
      dateRegistered: '2026-08-22',
      status: 'On Call',
    },
    {
      id: 'vol-004',
      fullName: 'Tariq Al-Mansoor',
      email: 'tariq.m@translation.net',
      phone: '+27 84 990 1284',
      availability: 'Remote Support',
      skills: ['Translation', 'Arabic', 'French', 'Documentation'],
      dateRegistered: '2026-08-21',
      status: 'Active',
    },
    {
      id: 'vol-005',
      fullName: 'Chloe Van Der Merwe',
      email: 'chloe.vdm@watertech.org',
      phone: '+27 82 119 4432',
      availability: 'Immediate Deployment',
      skills: ['Water Purification', 'Hydrology', 'Field Logistics'],
      dateRegistered: '2026-08-20',
      status: 'Active',
    },
  ];

  const initialUpdates = [
    {
      id: 'upd-001',
      project: 'Syria Relief',
      title: 'Mobile Trauma Unit Dispatched to Northern Region',
      message: 'Two specialized surgical trailers and 4,000 thermal blankets were delivered today to the field hospital in Idlib province.',
      author: 'Operations Command',
      date: '2026-08-27T08:00:00Z',
      region: 'Northern Syria',
    },
    {
      id: 'upd-002',
      project: 'KwaZulu-Natal Flood Relief',
      title: '5,000 Food Hampers & Potable Water Distributed',
      message: 'Rapid response convoy reached isolated rural villages cut off by bridge washaways in Tongaat and Inanda.',
      author: 'Regional Coordinator',
      date: '2026-08-26T15:30:00Z',
      region: 'South Africa',
    },
    {
      id: 'upd-003',
      project: 'Palestine Appeal',
      title: 'Medical Consumables and Surgical Sets Delivered',
      message: 'Emergency shipment containing anesthesia supplies, orthopedic fixators, and intensive burn treatment kits reached Nasser Medical Complex.',
      author: 'Medical Dispatch',
      date: '2026-08-25T19:00:00Z',
      region: 'Gaza Strip',
    },
    {
      id: 'upd-004',
      project: 'Clean Water Boreholes',
      title: 'High-Yield Solar Borehole Commissioned in Eastern Cape',
      message: 'Drilling reached 120m aquifer providing 18,000 liters per hour of tested, pathogen-free water to 3 schools and the local clinic.',
      author: 'Hydrology Division',
      date: '2026-08-24T12:00:00Z',
      region: 'Eastern Cape, SA',
    },
  ];

  const initiatives = [
    {
      id: 'init-001',
      title: 'Disaster Response',
      subtitle: 'Immediate action force',
      category: 'Disaster Response',
      region: 'Global & Sub-Saharan Africa',
      raisedZar: 14500000,
      targetZar: 20000000,
      activeVolunteers: 180,
      description: 'Rapid mobilization within hours of earthquakes, floods, wildfires, and conflict zones with high-tech rescue equipment and search dogs.',
      icon: 'DR',
      urgent: true,
    },
    {
      id: 'init-002',
      title: 'Medical Relief',
      subtitle: 'Field hospitals & surgical teams',
      category: 'Medical Relief',
      region: 'Gaza, Syria, Yemen, SA Hospitals',
      raisedZar: 9200000,
      targetZar: 12000000,
      activeVolunteers: 85,
      description: 'Equipping public hospitals with life-saving trauma equipment, deploying specialized surgical missions, and supplying vital medicines.',
      icon: 'MR',
    },
    {
      id: 'init-003',
      title: 'Volunteer Programs',
      subtitle: 'Field training',
      category: 'Volunteer Programs',
      region: 'Nationwide & Regional Hubs',
      raisedZar: 3400000,
      targetZar: 5000000,
      activeVolunteers: 342,
      description: 'Training community volunteers in advanced first aid, search & rescue, relief logistics, and emergency cooking facilities.',
      icon: 'VP',
    },
    {
      id: 'init-004',
      title: 'Community Dev',
      subtitle: 'Sustainable water & food security',
      category: 'Community Dev',
      region: 'Drought-prone rural zones',
      raisedZar: 7800000,
      targetZar: 10000000,
      activeVolunteers: 45,
      description: 'Solar-powered borehole drilling, agricultural eco-gardens, school upgrades, and infrastructure restoration for long-term resilience.',
      icon: 'CD',
    },
    {
      id: 'init-005',
      title: 'Sponsor a Family',
      subtitle: 'Direct nutritional & winter aid',
      category: 'Sponsor a Family',
      region: 'Displaced & Vulnerable Households',
      raisedZar: 4600000,
      targetZar: 6000000,
      activeVolunteers: 60,
      description: 'Monthly food parcels, hygiene kits, baby essentials, and thermal bedding for families in acute distress.',
      icon: 'SF',
    },
  ];

  let donations = initialDonations.slice();
  let volunteers = initialVolunteers.slice();
  let updates = initialUpdates.slice();

  const baseDonationUsd = 124500;
  const baseVolunteerCount = 43;

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function currencySymbol(currency) {
    return currencyConfig[currency]?.symbol || 'R';
  }

  function formatDonationAmount(amount, currency) {
    return `${currencySymbol(currency)}${Number(amount).toLocaleString()}`;
  }

  function convertDonationToUsd(record) {
    if (record.currency === 'ZAR') {
      return record.amount / 18;
    }
    if (record.currency === 'EUR') {
      return record.amount * 1.08;
    }
    return record.amount;
  }

  function showToast(message) {
    if (!toast || !toastMessage) {
      return;
    }

    toastMessage.textContent = message;
    toast.hidden = false;

    window.clearTimeout(showToast._timer);
    showToast._timer = window.setTimeout(() => {
      toast.hidden = true;
    }, 4500);
  }

  function hideToast() {
    if (toast) {
      toast.hidden = true;
    }
  }

  function renderInitiatives() {
    const grid = document.querySelector('[data-role="initiative-grid"]');
    if (!grid) {
      return;
    }

    const query = state.initiativeSearch.trim().toLowerCase();
    const filtered = initiatives.filter((item) => {
      const categoryMatch = state.initiativeCategory === 'All' || item.category === state.initiativeCategory;
      const searchMatch = !query || [item.title, item.description, item.region].some((value) => value.toLowerCase().includes(query));
      return categoryMatch && searchMatch;
    });

    if (filtered.length === 0) {
      grid.innerHTML = '<div class="initiative-card"><h3 class="initiative-card__title">No initiatives matched your search.</h3><p class="initiative-card__copy">Try a different category or keyword.</p></div>';
      return;
    }

    grid.innerHTML = filtered.map((item) => {
      const percent = Math.min(100, Math.round((item.raisedZar / item.targetZar) * 100));
      return `
        <article class="initiative-card">
          ${item.urgent ? '<span class="urgent-badge">Urgent Appeal</span>' : ''}
          <span class="initiative-card__icon ${item.urgent ? 'initiative-card__icon--alert' : ''}">${escapeHtml(item.icon)}</span>
          <h3 class="initiative-card__title">${escapeHtml(item.title)}</h3>
          <div class="initiative-card__meta">${escapeHtml(item.region)}</div>
          <p class="initiative-card__copy">${escapeHtml(item.description)}</p>
          <div class="initiative-card__footer">
            <div class="progress-copy">
              <strong>${escapeHtml(formatDonationAmount(item.raisedZar, 'ZAR'))} Raised</strong>
              <span>${percent}% of ${escapeHtml(formatDonationAmount(item.targetZar, 'ZAR'))} Goal</span>
            </div>
            <div class="progress-track" aria-hidden="true">
              <div class="progress-fill" style="width:${percent}%"></div>
            </div>
            <div class="progress-copy" style="margin-top:0.7rem;">
              <span>Active volunteers</span>
              <strong>${item.activeVolunteers} on duty</strong>
            </div>
            <div class="action-row" style="margin-top:0.9rem;">
              <button type="button" class="ui-button ui-button--primary" data-nav="donate">Donate to Initiative</button>
              <button type="button" class="ui-button ui-button--secondary" data-nav="volunteer">Join</button>
            </div>
          </div>
        </article>
      `;
    }).join('');
  }

  function donationStatusClass(status) {
    return status === 'Confirmed' ? 'status-badge--confirmed' : 'status-badge--pending';
  }

  function volunteerStatusClass(status) {
    if (status === 'In Field') {
      return 'status-badge--in-field';
    }
    if (status === 'On Call') {
      return 'status-badge--on-call';
    }
    return 'status-badge--active';
  }

  function renderRecentDonations() {
    const bodyEl = document.querySelector('[data-recent-donations]');
    if (!bodyEl) {
      return;
    }

    const search = state.dashboardSearch.trim().toLowerCase();
    const visible = donations.filter((donation) => {
      if (!search) {
        return true;
      }

      return [donation.id, donation.donorName, donation.email, donation.project, donation.amount, donation.currency]
        .some((value) => String(value).toLowerCase().includes(search));
    }).slice(0, 5);

    bodyEl.innerHTML = visible.map((donation) => `
      <tr>
        <td><strong>${escapeHtml(donation.donorName)}</strong></td>
        <td>${escapeHtml(formatDonationAmount(donation.amount, donation.currency))}</td>
        <td>${escapeHtml(donation.project)}</td>
        <td><span class="status-badge ${donationStatusClass(donation.status)}">${escapeHtml(donation.status)}</span></td>
      </tr>
    `).join('') || '<tr><td colspan="4">No donations found.</td></tr>';
  }

  function renderLedger() {
    const bodyEl = document.querySelector('[data-ledger-table]');
    if (!bodyEl) {
      return;
    }

    const search = state.dashboardSearch.trim().toLowerCase();
    const filtered = donations.filter((donation) => {
      const filterMatch = state.donationFilter === 'all' || donation.status === state.donationFilter;
      const searchMatch = !search || [donation.id, donation.donorName, donation.email, donation.project]
        .some((value) => String(value).toLowerCase().includes(search));
      return filterMatch && searchMatch;
    });

    bodyEl.innerHTML = filtered.map((donation) => `
      <tr>
        <td style="font-size:0.8rem;color:var(--muted);">${escapeHtml(donation.id)}</td>
        <td><strong>${escapeHtml(donation.donorName)}</strong></td>
        <td style="font-size:0.88rem;color:var(--muted);">${escapeHtml(donation.email)}</td>
        <td><strong>${escapeHtml(formatDonationAmount(donation.amount, donation.currency))}</strong></td>
        <td style="color:var(--olive);font-weight:700;">${escapeHtml(donation.project)}</td>
        <td>${escapeHtml(donation.frequency)}</td>
        <td><span class="status-badge ${donationStatusClass(donation.status)}">${escapeHtml(donation.status)}</span></td>
        <td style="text-align:right;">
          <button type="button" class="ghost-button" data-toggle-donation="${escapeHtml(donation.id)}">
            Toggle Status
          </button>
        </td>
      </tr>
    `).join('') || '<tr><td colspan="8">No donations match the current filter.</td></tr>';
  }

  function renderVolunteerRoster() {
    const bodyEl = document.querySelector('[data-volunteer-roster]');
    if (!bodyEl) {
      return;
    }

    const search = state.dashboardSearch.trim().toLowerCase();
    const visible = volunteers.filter((volunteer) => {
      if (!search) {
        return true;
      }

      return [volunteer.fullName, volunteer.email, volunteer.phone, volunteer.availability, volunteer.skills.join(' '), volunteer.status]
        .some((value) => String(value).toLowerCase().includes(search));
    });

    bodyEl.innerHTML = visible.map((volunteer) => `
      <article class="roster-card">
        <span class="roster-card__icon">${escapeHtml(volunteer.fullName.split(' ').map((name) => name[0]).join('').slice(0, 2).toUpperCase())}</span>
        <div style="display:flex;justify-content:space-between;gap:1rem;align-items:start;margin-top:0.8rem;">
          <div>
            <h3 class="roster-card__title" style="margin-top:0;">${escapeHtml(volunteer.fullName)}</h3>
            <div class="roster-card__meta">${escapeHtml(volunteer.email)} • ${escapeHtml(volunteer.phone)}</div>
            <div class="roster-card__copy" style="margin-top:0.45rem;">Availability: ${escapeHtml(volunteer.availability)}</div>
          </div>
          <span class="status-badge ${volunteerStatusClass(volunteer.status)}">${escapeHtml(volunteer.status)}</span>
        </div>
        <div class="initiative-card__footer" style="margin-top:1rem;">
          <div class="initiative-card__meta">Verified Skills</div>
          <div class="action-row" style="margin-top:0.55rem;">
            ${volunteer.skills.map((skill) => `<span class="status-badge status-badge--soft">${escapeHtml(skill)}</span>`).join('')}
          </div>
          <div class="check-row" style="justify-content:space-between;margin-top:0.9rem;">
            <span style="font-size:0.82rem;color:var(--muted);">Reg: ${escapeHtml(volunteer.dateRegistered)}</span>
            <div class="action-row">
              <button type="button" class="ghost-button" data-toggle-volunteer="${escapeHtml(volunteer.id)}" data-status="In Field">Deploy to Field</button>
              <button type="button" class="ghost-button" data-toggle-volunteer="${escapeHtml(volunteer.id)}" data-status="On Call">Set On Call</button>
            </div>
          </div>
        </div>
      </article>
    `).join('') || '<div class="initiative-card"><h3 class="initiative-card__title">No volunteers matched the search.</h3></div>';
  }

  function renderUpdatesFeed() {
    const bodyEl = document.querySelector('[data-updates-feed]');
    if (!bodyEl) {
      return;
    }

    const search = state.dashboardSearch.trim().toLowerCase();
    const visible = updates.filter((update) => {
      if (!search) {
        return true;
      }

      return [update.project, update.title, update.message, update.author, update.region]
        .some((value) => String(value).toLowerCase().includes(search));
    });

    bodyEl.innerHTML = visible.map((update) => `
      <article class="update-card">
        <div class="check-row" style="justify-content:space-between;align-items:flex-start;">
          <span class="status-badge status-badge--field">${escapeHtml(update.project)}</span>
          <span style="font-size:0.8rem;color:var(--muted);">${escapeHtml(new Date(update.date).toLocaleString())}</span>
        </div>
        <h3 class="update-card__title">${escapeHtml(update.title)}</h3>
        <p class="update-card__copy">${escapeHtml(update.message)}</p>
        <div class="update-card__meta">Author: ${escapeHtml(update.author)} • Region: ${escapeHtml(update.region)}</div>
      </article>
    `).join('') || '<div class="update-card"><h3 class="update-card__title">No updates found.</h3></div>';
  }

  function updateSummaryCounts() {
    const totalDonationEl = document.querySelector('[data-total-donations]');
    const volunteerCountEl = document.querySelector('[data-volunteer-count]');
    const activeProjectsEl = document.querySelector('[data-active-projects]');

    if (totalDonationEl) {
      const total = donations.reduce((acc, donation) => acc + convertDonationToUsd(donation), baseDonationUsd);
      totalDonationEl.textContent = `$${Math.round(total).toLocaleString()}`;
    }

    if (volunteerCountEl) {
      volunteerCountEl.textContent = String(volunteers.length + baseVolunteerCount);
    }

    if (activeProjectsEl) {
      activeProjectsEl.textContent = '12';
    }
  }

  function renderDonationUI() {
    const config = currencyConfig[state.currency];
    const symbols = Array.from(document.querySelectorAll('[data-amount-symbol]'));
    const prefix = document.querySelector('[data-custom-prefix]');
    const water = document.querySelector('[data-water-amount]');
    const medical = document.querySelector('[data-medical-amount]');
    const submitLabel = document.querySelector('[data-donation-submit-label]');
    const amountButtons = Array.from(document.querySelectorAll('[data-preset]'));
    const currencyButtons = Array.from(document.querySelectorAll('[data-currency]'));
    const frequencyButtons = Array.from(document.querySelectorAll('[data-frequency]'));

    symbols.forEach((node) => {
      node.textContent = config.symbol;
    });

    if (prefix) {
      prefix.textContent = config.symbol;
    }

    if (water) {
      water.textContent = config.water;
    }

    if (medical) {
      medical.textContent = config.medical;
    }

    amountButtons.forEach((button) => {
      const preset = Number(button.dataset.preset);
      const isActive = !state.customAmount && state.selectedPreset === preset;
      button.classList.toggle('is-active', isActive);
      button.innerHTML = `<span data-amount-symbol>${escapeHtml(config.symbol)}</span>${preset.toLocaleString()}`;
    });

    currencyButtons.forEach((button) => {
      button.classList.toggle('is-active', button.dataset.currency === state.currency);
    });

    frequencyButtons.forEach((button) => {
      button.classList.toggle('is-active', button.dataset.frequency === state.frequency);
    });

    if (submitLabel) {
      submitLabel.textContent = `Donate ${config.symbol}${getEffectiveDonationAmount().toLocaleString()} Now`;
    }
  }

  function renderVolunteerSkills() {
    const holder = document.querySelector('[data-selected-skills]');
    if (!holder) {
      return;
    }

    holder.innerHTML = state.volunteerSkills.map((skill) => `
      <span class="status-badge status-badge--soft" style="display:inline-flex;gap:0.35rem;align-items:center;">
        <span>${escapeHtml(skill)}</span>
        <button type="button" class="toast__close" data-remove-skill="${escapeHtml(skill)}" aria-label="Remove skill">×</button>
      </span>
    `).join('');
  }

  function getEffectiveDonationAmount() {
    if (state.customAmount && Number(state.customAmount) > 0) {
      return Number(state.customAmount);
    }
    return Number(state.selectedPreset || 0);
  }

  function setPage(page, options = {}) {
    const { updateHash = true, scrollToTop = true } = options;
    const nextPage = pages.some((section) => section.dataset.page === page) ? page : 'home';

    state.page = nextPage;

    pages.forEach((section) => {
      section.classList.toggle('is-active', section.dataset.page === nextPage);
    });

    navButtons.forEach((button) => {
      button.classList.toggle('is-active', button.dataset.nav === nextPage);
    });

    if (updateHash) {
      history.replaceState(null, '', `#${nextPage}`);
    }

    if (scrollToTop) {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  function setDashboardTab(tab) {
    state.dashboardTab = tab;

    document.querySelectorAll('[data-dashboard-pane]').forEach((pane) => {
      pane.classList.toggle('is-active', pane.dataset.dashboardPane === tab);
    });

    document.querySelectorAll('[data-dashboard-tab]').forEach((button) => {
      button.classList.toggle('is-active', button.dataset.dashboardTab === tab);
    });
  }

  function setInitiativeCategory(category) {
    state.initiativeCategory = category;
    document.querySelectorAll('[data-initiative-filter]').forEach((button) => {
      button.classList.toggle('is-active', button.dataset.initiativeFilter === category);
    });
    renderInitiatives();
  }

  function openModal(name) {
    const modal = document.querySelector(`[data-modal="${name}"]`);
    if (!modal) {
      return;
    }

    modal.classList.add('is-open');
    modal.setAttribute('aria-hidden', 'false');
    body.classList.add('menu-open');
  }

  function closeModal(name) {
    const modal = document.querySelector(`[data-modal="${name}"]`);
    if (!modal) {
      return;
    }

    modal.classList.remove('is-open');
    modal.setAttribute('aria-hidden', 'true');
    body.classList.remove('menu-open');
  }

  function closeMobileMenu() {
    if (mobileMenu) {
      mobileMenu.classList.remove('is-open');
    }
    body.classList.remove('menu-open');
  }

  function openMobileMenu() {
    if (mobileMenu) {
      mobileMenu.classList.add('is-open');
    }
    body.classList.add('menu-open');
  }

  function addSkill(skill) {
    const trimmed = String(skill || '').trim();
    if (!trimmed || state.volunteerSkills.includes(trimmed)) {
      return;
    }

    state.volunteerSkills = [...state.volunteerSkills, trimmed];
    renderVolunteerSkills();
  }

  function removeSkill(skill) {
    state.volunteerSkills = state.volunteerSkills.filter((item) => item !== skill);
    if (state.volunteerSkills.length === 0) {
      state.volunteerSkills = ['General Volunteer'];
    }
    renderVolunteerSkills();
  }

  function resetDonationForm() {
    const form = document.getElementById('donate-form');
    const success = document.querySelector('[data-donation-success]');
    if (!form || !success) {
      return;
    }

    state.donationSuccess = null;
    success.hidden = true;
    form.hidden = false;
    form.reset();
    state.customAmount = '';
    state.selectedPreset = currencyConfig[state.currency].presets[1];
    state.frequency = 'one-time';
    state.donationProject = 'General Relief Fund';
    state.donationTax = false;
    renderDonationUI();
  }

  function resetVolunteerForm() {
    const form = document.getElementById('volunteer-form');
    const success = document.querySelector('[data-volunteer-success]');
    if (!form || !success) {
      return;
    }

    state.volunteerSuccess = null;
    success.hidden = true;
    form.hidden = false;
    form.reset();
    state.volunteerSkills = ['Medical', 'Logistics'];
    renderVolunteerSkills();
  }

  function resetContactForm() {
    const form = document.getElementById('contact-form');
    const success = document.querySelector('[data-contact-success]');
    if (!form || !success) {
      return;
    }

    state.contactSuccess = null;
    success.hidden = true;
    form.hidden = false;
    form.reset();
  }

  function handleDonationSubmit(event) {
    event.preventDefault();

    const firstName = document.getElementById('donate-first-name');
    const lastName = document.getElementById('donate-last-name');
    const email = document.getElementById('donate-email');
    const tax = document.getElementById('donate-tax');
    const project = document.getElementById('donate-project');
    const form = document.getElementById('donate-form');
    const success = document.querySelector('[data-donation-success]');

    if (!firstName.value.trim() || !lastName.value.trim() || !email.value.trim()) {
      window.alert('Please provide your name and email address.');
      return;
    }

    const amount = getEffectiveDonationAmount();
    if (!amount || amount <= 0) {
      window.alert('Please select or enter a valid donation amount.');
      return;
    }

    const record = {
      id: `don-${String(Date.now()).slice(-4)}`,
      donorName: `${firstName.value.trim()} ${lastName.value.trim()}`,
      amount,
      currency: state.currency,
      project: project.value,
      status: 'Confirmed',
      date: new Date().toISOString(),
      email: email.value.trim(),
      frequency: state.frequency,
    };

    donations = [record, ...donations];
    state.donationSuccess = record;
    state.donationTax = Boolean(tax.checked);
    state.donationProject = project.value;

    if (form && success) {
      form.hidden = true;
      success.hidden = false;
      success.querySelector('[data-donation-first-name]').textContent = firstName.value.trim().split(/\s+/)[0];
      success.querySelector('[data-donation-amount]').textContent = formatDonationAmount(record.amount, record.currency);
      success.querySelector('[data-donation-project]').textContent = record.project;
      success.querySelector('[data-donation-ref]').textContent = record.id;
      success.querySelector('[data-donation-frequency]').textContent = record.frequency;
      success.querySelector('[data-donation-email]').textContent = record.email;
      const taxRow = success.querySelector('[data-donation-tax-row]');
      taxRow.hidden = !state.donationTax;
    }

    renderRecentDonations();
    renderLedger();
    updateSummaryCounts();
    showToast(`New donation of ${formatDonationAmount(record.amount, record.currency)} received from ${record.donorName}!`);
  }

  function handleVolunteerSubmit(event) {
    event.preventDefault();

    const fullName = document.getElementById('volunteer-name');
    const email = document.getElementById('volunteer-email-input');
    const phone = document.getElementById('volunteer-phone');
    const availability = document.getElementById('volunteer-availability');
    const form = document.getElementById('volunteer-form');
    const success = document.querySelector('[data-volunteer-success]');

    if (!fullName.value.trim() || !email.value.trim()) {
      window.alert('Please enter your full name and email address.');
      return;
    }

    const record = {
      id: `vol-${String(Date.now()).slice(-4)}`,
      fullName: fullName.value.trim(),
      email: email.value.trim(),
      phone: phone.value.trim() || '+27 (0)00 000 0000',
      availability: availability.value,
      skills: state.volunteerSkills.length ? state.volunteerSkills.slice() : ['General Volunteer'],
      dateRegistered: new Date().toISOString().slice(0, 10),
      status: availability.value === 'Immediate Deployment' ? 'In Field' : 'Active',
    };

    volunteers = [record, ...volunteers];
    state.volunteerSuccess = record;

    if (form && success) {
      form.hidden = true;
      success.hidden = false;
      success.querySelector('[data-volunteer-name]').textContent = record.fullName;
      success.querySelector('[data-volunteer-id]').textContent = record.id;
      success.querySelector('[data-volunteer-availability]').textContent = record.availability;
      success.querySelector('[data-volunteer-email]').textContent = record.email;
      const skillHolder = success.querySelector('[data-volunteer-skills]');
      skillHolder.innerHTML = record.skills.map((skill) => `<span class="status-badge status-badge--soft">${escapeHtml(skill)}</span>`).join('');
    }

    renderVolunteerRoster();
    updateSummaryCounts();
    showToast(`Volunteer ${record.fullName} has joined the response network!`);
  }

  function handleContactSubmit(event) {
    event.preventDefault();

    const name = document.getElementById('contact-name');
    const email = document.getElementById('contact-email');
    const message = document.getElementById('contact-message');
    const form = document.getElementById('contact-form');
    const success = document.querySelector('[data-contact-success]');

    if (!name.value.trim() || !email.value.trim() || !message.value.trim()) {
      window.alert('Please fill out all required fields.');
      return;
    }

    state.contactSuccess = {
      name: name.value.trim(),
      email: email.value.trim(),
    };

    if (form && success) {
      form.hidden = true;
      success.hidden = false;
      success.querySelector('[data-contact-name]').textContent = state.contactSuccess.name;
      success.querySelector('[data-contact-email]').textContent = state.contactSuccess.email;
    }

    showToast('Message dispatched to operations.');
  }

  function handleUpdateSubmit(event) {
    event.preventDefault();

    const project = document.getElementById('update-project');
    const title = document.getElementById('update-title');
    const message = document.getElementById('update-message');
    const success = document.querySelector('[data-update-success]');

    if (!title.value.trim() || !message.value.trim()) {
      window.alert('Please fill out all fields.');
      return;
    }

    const record = {
      id: `upd-${String(Date.now()).slice(-4)}`,
      project: project.value,
      title: title.value.trim(),
      message: message.value.trim(),
      author: 'Sarah Jenkins (Ops Dispatch)',
      date: new Date().toISOString(),
      region: 'Global Command',
    };

    updates = [record, ...updates];

    if (success) {
      success.hidden = false;
      window.setTimeout(() => {
        success.hidden = true;
      }, 3000);
    }

    title.value = '';
    message.value = '';
    renderUpdatesFeed();
    showToast(`Field update published: "${record.title}"`);
  }

  function exportDonationsCsv() {
    const rows = [
      ['ID', 'Donor Name', 'Email', 'Amount', 'Currency', 'Project', 'Status', 'Date'],
      ...donations.map((donation) => [
        donation.id,
        `"${donation.donorName}"`,
        donation.email,
        donation.amount,
        donation.currency,
        `"${donation.project}"`,
        donation.status,
        donation.date,
      ]),
    ];

    const blob = new Blob([rows.map((row) => row.join(',')).join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `donations_export_${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  function handleShare() {
    const shareData = {
      title: 'Gift of the Givers - Disaster Relief',
      text: 'Delivering relief faster, where it matters most.',
      url: window.location.href,
    };

    if (navigator.share) {
      navigator.share(shareData).catch(() => {});
      return;
    }

    navigator.clipboard.writeText(window.location.href).then(() => {
      showToast('Link copied to clipboard.');
    }).catch(() => {
      showToast('Unable to copy the link right now.');
    });
  }

  function updateDonateFromInputs() {
    const customAmount = document.getElementById('donate-custom-amount');
    const project = document.getElementById('donate-project');
    const tax = document.getElementById('donate-tax');

    state.customAmount = customAmount.value.trim();
    state.donationProject = project.value;
    state.donationTax = tax.checked;
    renderDonationUI();
  }

  function bindEvents() {
    document.addEventListener('click', (event) => {
      const nav = event.target.closest('[data-nav]');
      if (nav) {
        const page = nav.dataset.nav;
        setPage(page);
        closeMobileMenu();
        if (scheduleModal && scheduleModal.classList.contains('is-open')) {
          closeModal('schedule');
        }
        return;
      }

      const openModalButton = event.target.closest('[data-open-modal]');
      if (openModalButton) {
        openModal(openModalButton.dataset.openModal);
        return;
      }

      const closeModalButton = event.target.closest('[data-close-modal]');
      if (closeModalButton) {
        closeModal(closeModalButton.dataset.closeModal);
        return;
      }

      const removeSkillButton = event.target.closest('[data-remove-skill]');
      if (removeSkillButton) {
        removeSkill(removeSkillButton.dataset.removeSkill);
        return;
      }

      const addSkillButton = event.target.closest('[data-add-skill]');
      if (addSkillButton) {
        const input = document.getElementById('volunteer-skill-input');
        addSkill(input.value);
        input.value = '';
        return;
      }

      const suggestedSkillButton = event.target.closest('[data-suggest-skill]');
      if (suggestedSkillButton) {
        addSkill(suggestedSkillButton.dataset.suggestSkill);
        const form = document.getElementById('volunteer-form');
        if (form) {
          form.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
        return;
      }

      const suggestRoleButton = event.target.closest('[data-suggest-role]');
      if (suggestRoleButton) {
        addSkill(suggestRoleButton.dataset.suggestRole);
        const input = document.getElementById('volunteer-skill-input');
        if (input) {
          input.value = suggestRoleButton.dataset.suggestRole;
        }
        const form = document.getElementById('volunteer-form');
        if (form) {
          form.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
        return;
      }

      const presetButton = event.target.closest('[data-preset]');
      if (presetButton) {
        state.selectedPreset = Number(presetButton.dataset.preset);
        state.customAmount = '';
        const customInput = document.getElementById('donate-custom-amount');
        if (customInput) {
          customInput.value = '';
        }
        renderDonationUI();
        return;
      }

      const currencyButton = event.target.closest('[data-currency]');
      if (currencyButton) {
        state.currency = currencyButton.dataset.currency;
        state.selectedPreset = currencyConfig[state.currency].presets[1];
        state.customAmount = '';
        const customInput = document.getElementById('donate-custom-amount');
        if (customInput) {
          customInput.value = '';
        }
        renderDonationUI();
        return;
      }

      const frequencyButton = event.target.closest('[data-frequency]');
      if (frequencyButton) {
        state.frequency = frequencyButton.dataset.frequency;
        renderDonationUI();
        return;
      }

      const donationToggle = event.target.closest('[data-toggle-donation]');
      if (donationToggle) {
        const id = donationToggle.dataset.toggleDonation;
        donations = donations.map((donation) => {
          if (donation.id !== id) {
            return donation;
          }

          return {
            ...donation,
            status: donation.status === 'Confirmed' ? 'Pending' : 'Confirmed',
          };
        });
        renderRecentDonations();
        renderLedger();
        updateSummaryCounts();
        return;
      }

      const volunteerToggle = event.target.closest('[data-toggle-volunteer]');
      if (volunteerToggle) {
        const id = volunteerToggle.dataset.toggleVolunteer;
        const nextStatus = volunteerToggle.dataset.status;
        volunteers = volunteers.map((volunteer) => {
          if (volunteer.id !== id) {
            return volunteer;
          }
          return {
            ...volunteer,
            status: nextStatus,
          };
        });
        renderVolunteerRoster();
        updateSummaryCounts();
        return;
      }

      const toggleTab = event.target.closest('[data-dashboard-tab]');
      if (toggleTab) {
        setDashboardTab(toggleTab.dataset.dashboardTab);
        return;
      }

      const donationFilter = event.target.closest('[data-donation-filter]');
      if (donationFilter) {
        state.donationFilter = donationFilter.dataset.donationFilter;
        document.querySelectorAll('[data-donation-filter]').forEach((button) => {
          button.classList.toggle('is-active', button.dataset.donationFilter === state.donationFilter);
        });
        renderLedger();
        return;
      }

      const exportButton = event.target.closest('[data-export-donations]');
      if (exportButton) {
        exportDonationsCsv();
        return;
      }

      const downloadReceipt = event.target.closest('[data-download-receipt]');
      if (downloadReceipt) {
        window.alert('Receipt downloaded successfully.');
        return;
      }

      const donationReset = event.target.closest('[data-reset-donation]');
      if (donationReset) {
        resetDonationForm();
        return;
      }

      const volunteerReset = event.target.closest('[data-volunteer-reset]');
      if (volunteerReset) {
        resetVolunteerForm();
        return;
      }

      const contactReset = event.target.closest('[data-contact-reset]');
      if (contactReset) {
        resetContactForm();
        return;
      }

      const toastDismiss = event.target.closest('[data-toast-close]');
      if (toastDismiss) {
        hideToast();
        return;
      }

      const shareButton = event.target.closest('[data-share]');
      if (shareButton) {
        handleShare();
        return;
      }

      const initiativeFilter = event.target.closest('[data-initiative-filter]');
      if (initiativeFilter) {
        setInitiativeCategory(initiativeFilter.dataset.initiativeFilter);
      }
    });

    document.addEventListener('submit', (event) => {
      if (event.target.id === 'donate-form') {
        handleDonationSubmit(event);
      } else if (event.target.id === 'volunteer-form') {
        handleVolunteerSubmit(event);
      } else if (event.target.id === 'contact-form') {
        handleContactSubmit(event);
      } else if (event.target.id === 'update-form') {
        handleUpdateSubmit(event);
      } else if (event.target.matches('[data-newsletter-form]')) {
        event.preventDefault();
        const input = document.querySelector('[data-newsletter-input]');
        if (!input.value.trim()) {
          return;
        }
        showToast('You are subscribed to field situation reports.');
        input.value = '';
      }
    });

    document.addEventListener('input', (event) => {
      if (event.target.matches('[data-initiative-search]')) {
        state.initiativeSearch = event.target.value;
        renderInitiatives();
      }

      if (event.target.matches('[data-dashboard-search]')) {
        state.dashboardSearch = event.target.value;
        renderRecentDonations();
        renderLedger();
        renderVolunteerRoster();
        renderUpdatesFeed();
      }

      if (event.target.id === 'donate-custom-amount') {
        state.customAmount = event.target.value;
        renderDonationUI();
      }
    });

    document.addEventListener('change', (event) => {
      if (event.target.id === 'donate-project') {
        state.donationProject = event.target.value;
        renderDonationUI();
      }

      if (event.target.id === 'donate-tax') {
        state.donationTax = event.target.checked;
      }
    });

    document.addEventListener('keydown', (event) => {
      if (event.target.id === 'volunteer-skill-input' && (event.key === 'Enter' || event.key === ',')) {
        event.preventDefault();
        const input = document.getElementById('volunteer-skill-input');
        addSkill(input.value);
        input.value = '';
      }

      if (event.key === 'Escape' && scheduleModal && scheduleModal.classList.contains('is-open')) {
        closeModal('schedule');
      }
    });

    if (menuToggle) {
      menuToggle.addEventListener('click', () => {
        if (mobileMenu.classList.contains('is-open')) {
          closeMobileMenu();
        } else {
          openMobileMenu();
        }
      });
    }

    window.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') {
        closeMobileMenu();
      }
    });
  }

  function initialize() {
    if (currentYear) {
      currentYear.textContent = String(new Date().getFullYear());
    }

    renderInitiatives();
    renderRecentDonations();
    renderLedger();
    renderVolunteerRoster();
    renderUpdatesFeed();
    updateSummaryCounts();
    renderVolunteerSkills();
    renderDonationUI();
    setDashboardTab('overview');

    const hashPage = window.location.hash.replace('#', '').trim();
    const initialPage = pages.some((section) => section.dataset.page === hashPage) ? hashPage : 'home';
    setPage(initialPage, { updateHash: false, scrollToTop: false });

    if (toastClose) {
      toastClose.addEventListener('click', hideToast);
    }

    bindEvents();
  }

  initialize();
})();

const defaultTerms = {
  currency: "EUR",
  advanceRate: 85,
  discountRate: 1.45,
  serviceFee: 0.35,
  minimumFee: 75,
  bankApr: 12,
  model: "recourse"
};

const sampleInvoices = [
  { debtor: "Adriatic Retail Group", invoice: "INV-2026-1042", amount: 42000, days: 34, rating: "A", concentration: 22, status: "clean", fundingStage: "approved" },
  { debtor: "Northline Pharma", invoice: "INV-2026-1051", amount: 63500, days: 58, rating: "B", concentration: 31, status: "clean", fundingStage: "review" },
  { debtor: "MetroBuild Supply", invoice: "INV-2026-1077", amount: 21800, days: 76, rating: "C", concentration: 18, status: "unverified", fundingStage: "submitted" },
  { debtor: "GreenGrid Energy", invoice: "INV-2026-1083", amount: 87000, days: 45, rating: "A", concentration: 29, status: "clean", fundingStage: "funded" },
  { debtor: "Luma Foods", invoice: "INV-2026-1098", amount: 14200, days: 92, rating: "D", concentration: 7, status: "disputed", fundingStage: "draft" }
];

const ratingRisk = { A: 4, B: 12, C: 24, D: 40 };
const statusRisk = { clean: 0, unverified: 12, disputed: 35, overdue: 28 };
const validFundingStages = ["draft", "submitted", "review", "approved", "funded", "collected", "settled"];
const fundingStages = [
  { id: "draft", label: "Draft" },
  { id: "submitted", label: "Submitted" },
  { id: "review", label: "Review" },
  { id: "approved", label: "Approved" },
  { id: "funded", label: "Funded" },
  { id: "collected", label: "Collected" },
  { id: "settled", label: "Settled" }
];

const state = {
  terms: { ...defaultTerms },
  invoices: []
};

const els = {
  termsForm: document.querySelector("#termsForm"),
  currency: document.querySelector("#currency"),
  advanceRate: document.querySelector("#advanceRate"),
  discountRate: document.querySelector("#discountRate"),
  serviceFee: document.querySelector("#serviceFee"),
  minimumFee: document.querySelector("#minimumFee"),
  bankApr: document.querySelector("#bankApr"),
  rows: document.querySelector("#invoiceRows"),
  rowTemplate: document.querySelector("#rowTemplate"),
  addInvoiceBtn: document.querySelector("#addInvoiceBtn"),
  submitEligibleBtn: document.querySelector("#submitEligibleBtn"),
  loadSampleBtn: document.querySelector("#loadSampleBtn"),
  resetTermsBtn: document.querySelector("#resetTermsBtn"),
  exportBtn: document.querySelector("#exportBtn"),
  csvInput: document.querySelector("#csvInput"),
  copyOfferBtn: document.querySelector("#copyOfferBtn"),
  eligibleValue: document.querySelector("#eligibleValue"),
  eligibleCount: document.querySelector("#eligibleCount"),
  cashTodayValue: document.querySelector("#cashTodayValue"),
  advanceRateLabel: document.querySelector("#advanceRateLabel"),
  costValue: document.querySelector("#costValue"),
  effectiveAprValue: document.querySelector("#effectiveAprValue"),
  qualityValue: document.querySelector("#qualityValue"),
  qualityLabel: document.querySelector("#qualityLabel"),
  weightedDaysLabel: document.querySelector("#weightedDaysLabel"),
  advanceBar: document.querySelector("#advanceBar"),
  reserveBar: document.querySelector("#reserveBar"),
  feeBar: document.querySelector("#feeBar"),
  advanceSplit: document.querySelector("#advanceSplit"),
  reserveSplit: document.querySelector("#reserveSplit"),
  feeSplit: document.querySelector("#feeSplit"),
  signalsList: document.querySelector("#signalsList"),
  offerText: document.querySelector("#offerText"),
  chart: document.querySelector("#cashChart"),
  workflowGrid: document.querySelector("#workflowGrid"),
  pipelineValue: document.querySelector("#pipelineValue")
};

function money(value) {
  return `${state.terms.currency} ${Math.round(value).toLocaleString("en-US")}`;
}

function percent(value) {
  if (!Number.isFinite(value)) return "0%";
  return `${value.toFixed(value >= 10 ? 1 : 2)}%`;
}

function toNumber(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function invoiceRisk(invoice) {
  const dayRisk = Math.max(0, (invoice.days - 30) * 0.28);
  const concentrationRisk = Math.max(0, invoice.concentration - 25) * 0.7;
  const base = ratingRisk[invoice.rating] ?? 25;
  const status = statusRisk[invoice.status] ?? 0;
  const modelAdjustment = state.terms.model === "nonrecourse" ? 5 : 0;
  return Math.min(100, Math.round(base + dayRisk + concentrationRisk + status + modelAdjustment));
}

function invoiceScore(invoice) {
  return Math.max(0, 100 - invoiceRisk(invoice));
}

function invoiceEligibility(invoice) {
  const score = invoiceScore(invoice);
  const blockers = [];
  const warnings = [];

  if (invoice.amount <= 0) blockers.push("Missing amount");
  if (invoice.days > 120) blockers.push("Tenor above 120 days");
  if (invoice.status === "disputed") blockers.push("Disputed invoice");
  if (score < 48) blockers.push(`Risk score below 48 (${score})`);

  if (invoice.status === "unverified") warnings.push("Debtor confirmation needed");
  if (invoice.status === "overdue") warnings.push("Overdue collection risk");
  if (invoice.days > 90 && invoice.days <= 120) warnings.push("Long tenor review");
  if (invoice.concentration > 35) warnings.push("High debtor concentration");
  if (invoice.rating === "D" && score >= 48) warnings.push("Weak debtor rating");

  return {
    eligible: blockers.length === 0,
    blockers,
    warnings,
    reasons: blockers.length || warnings.length ? [...blockers, ...warnings] : ["Ready to fund"]
  };
}

function isEligible(invoice) {
  return invoiceEligibility(invoice).eligible;
}

function calculate() {
  const eligible = state.invoices.filter(isEligible);
  const eligibleAmount = eligible.reduce((sum, item) => sum + item.amount, 0);
  const totalAmount = state.invoices.reduce((sum, item) => sum + item.amount, 0);
  const advance = eligibleAmount * (state.terms.advanceRate / 100);
  const reserve = eligibleAmount - advance;
  const weightedDays = eligibleAmount
    ? eligible.reduce((sum, item) => sum + item.amount * item.days, 0) / eligibleAmount
    : 0;
  const rawDiscount = eligible.reduce((sum, item) => {
    return sum + item.amount * (state.terms.discountRate / 100) * (item.days / 30);
  }, 0);
  const service = eligibleAmount * (state.terms.serviceFee / 100);
  const modelPremium = state.terms.model === "nonrecourse" ? eligibleAmount * 0.004 : 0;
  const totalCost = eligibleAmount ? Math.max(state.terms.minimumFee, rawDiscount + service + modelPremium) : 0;
  const cashToday = Math.max(0, advance - totalCost);
  const effectiveApr = advance && weightedDays ? (totalCost / advance) * (365 / weightedDays) * 100 : 0;
  const bankCost = advance * (state.terms.bankApr / 100) * (weightedDays / 365);
  const quality = state.invoices.length
    ? Math.round(state.invoices.reduce((sum, item) => sum + invoiceScore(item), 0) / state.invoices.length)
    : 0;

  return {
    eligible,
    eligibleAmount,
    totalAmount,
    advance,
    reserve,
    weightedDays,
    totalCost,
    cashToday,
    effectiveApr,
    bankCost,
    quality
  };
}

function syncTermsFromInputs() {
  state.terms.currency = els.currency.value;
  state.terms.advanceRate = toNumber(els.advanceRate.value, defaultTerms.advanceRate);
  state.terms.discountRate = toNumber(els.discountRate.value, defaultTerms.discountRate);
  state.terms.serviceFee = toNumber(els.serviceFee.value, defaultTerms.serviceFee);
  state.terms.minimumFee = toNumber(els.minimumFee.value, defaultTerms.minimumFee);
  state.terms.bankApr = toNumber(els.bankApr.value, defaultTerms.bankApr);
  save();
  renderSummary();
}

function renderTerms() {
  els.currency.value = state.terms.currency;
  els.advanceRate.value = state.terms.advanceRate;
  els.discountRate.value = state.terms.discountRate;
  els.serviceFee.value = state.terms.serviceFee;
  els.minimumFee.value = state.terms.minimumFee;
  els.bankApr.value = state.terms.bankApr;
  document.querySelectorAll(".segment").forEach((button) => {
    button.classList.toggle("active", button.dataset.model === state.terms.model);
  });
}

function renderRows() {
  els.rows.innerHTML = "";
  state.invoices.forEach((invoice, index) => {
    const fragment = els.rowTemplate.content.cloneNode(true);
    const row = fragment.querySelector("tr");
    row.querySelector(".debtor-input").value = invoice.debtor;
    row.querySelector(".invoice-input").value = invoice.invoice;
    row.querySelector(".amount-input").value = invoice.amount;
    row.querySelector(".days-input").value = invoice.days;
    row.querySelector(".rating-input").value = invoice.rating;
    row.querySelector(".concentration-input").value = invoice.concentration;
    row.querySelector(".status-input").value = invoice.status;
    row.querySelector(".workflow-input").value = invoice.fundingStage;
    updateRowDecision(row, invoice);

    row.addEventListener("input", () => {
      state.invoices[index] = readRow(row);
      updateRowDecision(row, state.invoices[index]);
      save();
      renderSummary();
    });
    row.addEventListener("change", () => {
      state.invoices[index] = readRow(row);
      updateRowDecision(row, state.invoices[index]);
      save();
      renderSummary();
    });
    row.querySelector(".remove-button").addEventListener("click", () => {
      state.invoices.splice(index, 1);
      save();
      render();
    });
    els.rows.appendChild(fragment);
  });
}

function updateRowDecision(row, invoice) {
  const score = invoiceScore(invoice);
  const decision = invoiceEligibility(invoice);
  const pill = row.querySelector(".score-pill");
  pill.textContent = score;
  pill.style.background = score >= 75 ? "#e7f3e8" : score >= 50 ? "#fff1de" : "#fde7e7";
  pill.style.color = score >= 75 ? "#226638" : score >= 50 ? "#915015" : "#9a2626";

  const reasonList = row.querySelector(".reason-list");
  reasonList.innerHTML = decision.reasons
    .map((reason) => {
      const level = decision.blockers.includes(reason) ? "blocker" : decision.warnings.includes(reason) ? "warning" : "ready";
      return `<span class="reason-chip ${level}">${reason}</span>`;
    })
    .join("");
}

function readRow(row) {
  return {
    debtor: row.querySelector(".debtor-input").value.trim(),
    invoice: row.querySelector(".invoice-input").value.trim(),
    amount: toNumber(row.querySelector(".amount-input").value),
    days: toNumber(row.querySelector(".days-input").value, 1),
    rating: row.querySelector(".rating-input").value,
    concentration: toNumber(row.querySelector(".concentration-input").value),
    status: row.querySelector(".status-input").value,
    fundingStage: row.querySelector(".workflow-input").value
  };
}

function renderMetrics(summary) {
  els.eligibleValue.textContent = money(summary.eligibleAmount);
  els.eligibleCount.textContent = `${summary.eligible.length} of ${state.invoices.length} invoices`;
  els.cashTodayValue.textContent = money(summary.cashToday);
  els.advanceRateLabel.textContent = `${state.terms.advanceRate}% advance`;
  els.costValue.textContent = money(summary.totalCost);
  els.effectiveAprValue.textContent = `${percent(summary.effectiveApr)} effective APR`;
  els.qualityValue.textContent = summary.quality || 0;
  els.qualityLabel.textContent = summary.quality >= 75 ? "Strong" : summary.quality >= 55 ? "Watchlist" : state.invoices.length ? "High risk" : "No data";
  els.weightedDaysLabel.textContent = `${Math.round(summary.weightedDays)} days`;

  const total = Math.max(summary.eligibleAmount, 1);
  const advancePct = (summary.advance / total) * 100;
  const reservePct = (summary.reserve / total) * 100;
  const feePct = (summary.totalCost / total) * 100;
  els.advanceBar.style.width = `${Math.min(100, advancePct)}%`;
  els.reserveBar.style.width = `${Math.min(100, reservePct)}%`;
  els.feeBar.style.width = `${Math.min(100, feePct * 3)}%`;
  els.advanceSplit.textContent = percent(advancePct);
  els.reserveSplit.textContent = percent(reservePct);
  els.feeSplit.textContent = percent(feePct);
}

function renderSignals(summary) {
  const signals = [];
  const disputed = state.invoices.filter((item) => item.status === "disputed").length;
  const overdue = state.invoices.filter((item) => item.status === "overdue").length;
  const highConcentration = state.invoices.filter((item) => item.concentration > 35).length;
  const longTenor = state.invoices.filter((item) => item.days > 90).length;
  const ineligible = state.invoices.length - summary.eligible.length;

  if (!state.invoices.length) {
    signals.push({ level: "", text: "Add invoices to calculate eligibility and pricing." });
  } else {
    signals.push({ level: summary.quality >= 70 ? "" : "warn", text: `${summary.eligible.length} invoices pass current eligibility rules.` });
  }
  if (ineligible > 0) signals.push({ level: "warn", text: `${ineligible} invoices need review before funding.` });
  if (disputed > 0) signals.push({ level: "danger", text: `${disputed} disputed invoices are excluded from the offer.` });
  if (overdue > 0) signals.push({ level: "danger", text: `${overdue} overdue invoices add recourse risk.` });
  if (highConcentration > 0) signals.push({ level: "warn", text: `${highConcentration} invoices exceed 35% debtor concentration.` });
  if (longTenor > 0) signals.push({ level: "warn", text: `${longTenor} invoices have tenors above 90 days.` });
  if (summary.bankCost && summary.totalCost) {
    const difference = summary.totalCost - summary.bankCost;
    signals.push({
      level: difference > 0 ? "warn" : "",
      text: `Compared with the bank APR input, factoring is ${money(Math.abs(difference))} ${difference > 0 ? "more expensive" : "cheaper"} for the same period.`
    });
  }

  els.signalsList.innerHTML = signals
    .map((signal) => `<li class="${signal.level}">${signal.text}</li>`)
    .join("");
}

function renderWorkflow(summary) {
  const active = state.invoices.filter((invoice) => invoice.fundingStage !== "draft" && invoice.fundingStage !== "settled").length;
  els.pipelineValue.textContent = `${active} active`;
  els.workflowGrid.innerHTML = fundingStages
    .map((stage) => {
      const invoices = state.invoices.filter((invoice) => invoice.fundingStage === stage.id);
      const amount = invoices.reduce((sum, invoice) => sum + invoice.amount, 0);
      const eligible = invoices.filter(isEligible).length;
      return `
        <article class="workflow-card">
          <span>${stage.label}</span>
          <strong>${invoices.length}</strong>
          <small>${money(amount)} / ${eligible} eligible</small>
        </article>
      `;
    })
    .join("");
}

function renderOffer(summary) {
  const rejected = state.invoices.filter((item) => !isEligible(item));
  const pipelineInvoices = state.invoices.filter((item) => item.fundingStage !== "draft");
  const lines = [
    "INDICATIVE FACTORING TERMS",
    "",
    `Model: ${state.terms.model === "nonrecourse" ? "Non-recourse" : "Recourse"}`,
    `Eligible receivables: ${money(summary.eligibleAmount)}`,
    `Advance rate: ${state.terms.advanceRate}%`,
    `Gross advance: ${money(summary.advance)}`,
    `Estimated fees: ${money(summary.totalCost)}`,
    `Cash today: ${money(summary.cashToday)}`,
    `Reserve at collection: ${money(summary.reserve)}`,
    `Weighted tenor: ${Math.round(summary.weightedDays)} days`,
    `Effective APR: ${percent(summary.effectiveApr)}`,
    "",
    "UNDERWRITING CONDITIONS",
    "- Debtor confirmation before funding",
    "- No undisclosed disputes, set-offs, or credit notes",
    "- Assignment notice sent on funded invoices",
    "- Final offer subject to KYC and receivable verification"
  ];

  if (pipelineInvoices.length) {
    lines.push("", "FUNDING PIPELINE");
    fundingStages.forEach((stage) => {
      const invoices = pipelineInvoices.filter((invoice) => invoice.fundingStage === stage.id);
      if (invoices.length) lines.push(`- ${stage.label}: ${invoices.length} invoice(s)`);
    });
  }

  if (rejected.length) {
    lines.push("", "EXCLUDED INVOICES");
    rejected.forEach((invoice) => {
      const reasons = invoiceEligibility(invoice).blockers.join("; ");
      lines.push(`- ${invoice.invoice || "Unnumbered"} / ${invoice.debtor || "Unknown debtor"} / score ${invoiceScore(invoice)} / ${reasons}`);
    });
  }

  els.offerText.textContent = lines.join("\n");
}

function drawChart(summary) {
  const canvas = els.chart;
  const ctx = canvas.getContext("2d");
  const rect = canvas.getBoundingClientRect();
  const scale = window.devicePixelRatio || 1;
  canvas.width = Math.max(1, Math.floor(rect.width * scale));
  canvas.height = Math.max(1, Math.floor(rect.height * scale));
  ctx.scale(scale, scale);

  const width = rect.width;
  const height = rect.height;
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(0, 0, width, height);

  const padding = { top: 26, right: 24, bottom: 44, left: 64 };
  const chartW = width - padding.left - padding.right;
  const chartH = height - padding.top - padding.bottom;
  const maxValue = Math.max(summary.cashToday, summary.reserve, summary.totalCost, 1);
  const bars = [
    { label: "Today", value: summary.cashToday, color: "#166b5b" },
    { label: "Reserve", value: summary.reserve, color: "#2d7fbc" },
    { label: "Fees", value: summary.totalCost, color: "#bc6c25" }
  ];

  ctx.strokeStyle = "#d9ded6";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(padding.left, padding.top);
  ctx.lineTo(padding.left, padding.top + chartH);
  ctx.lineTo(padding.left + chartW, padding.top + chartH);
  ctx.stroke();

  ctx.fillStyle = "#69736f";
  ctx.font = "12px system-ui, sans-serif";
  ctx.textAlign = "right";
  for (let i = 0; i <= 4; i += 1) {
    const y = padding.top + chartH - (chartH * i) / 4;
    const value = (maxValue * i) / 4;
    ctx.strokeStyle = "#edf0ec";
    ctx.beginPath();
    ctx.moveTo(padding.left, y);
    ctx.lineTo(padding.left + chartW, y);
    ctx.stroke();
    ctx.fillText(compactMoney(value), padding.left - 10, y + 4);
  }

  const barW = Math.min(90, chartW / 5);
  const gap = (chartW - barW * bars.length) / (bars.length + 1);
  bars.forEach((bar, index) => {
    const x = padding.left + gap + index * (barW + gap);
    const barH = (bar.value / maxValue) * chartH;
    const y = padding.top + chartH - barH;
    roundRect(ctx, x, y, barW, barH, 7, bar.color);
    ctx.fillStyle = "#18211f";
    ctx.textAlign = "center";
    ctx.font = "700 12px system-ui, sans-serif";
    ctx.fillText(bar.label, x + barW / 2, padding.top + chartH + 24);
    ctx.fillStyle = "#69736f";
    ctx.font = "12px system-ui, sans-serif";
    ctx.fillText(compactMoney(bar.value), x + barW / 2, Math.max(16, y - 8));
  });
}

function roundRect(ctx, x, y, width, height, radius, color) {
  const r = Math.min(radius, Math.abs(height) / 2, width / 2);
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + width, y, x + width, y + height, r);
  ctx.arcTo(x + width, y + height, x, y + height, r);
  ctx.arcTo(x, y + height, x, y, r);
  ctx.arcTo(x, y, x + width, y, r);
  ctx.closePath();
  ctx.fill();
}

function compactMoney(value) {
  if (value >= 1000000) return `${state.terms.currency} ${(value / 1000000).toFixed(1)}m`;
  if (value >= 1000) return `${state.terms.currency} ${(value / 1000).toFixed(0)}k`;
  return `${state.terms.currency} ${Math.round(value)}`;
}

function render() {
  renderTerms();
  renderRows();
  renderSummary();
}

function renderSummary() {
  const summary = calculate();
  renderMetrics(summary);
  renderSignals(summary);
  renderWorkflow(summary);
  renderOffer(summary);
  drawChart(summary);
}

function addInvoice(invoice = {}) {
  state.invoices.push(normalizeInvoice({
    debtor: invoice.debtor ?? "New debtor",
    invoice: invoice.invoice ?? `INV-${new Date().getFullYear()}-${String(state.invoices.length + 1).padStart(4, "0")}`,
    amount: invoice.amount ?? 10000,
    days: invoice.days ?? 45,
    rating: invoice.rating ?? "B",
    concentration: invoice.concentration ?? 20,
    status: invoice.status ?? "clean",
    fundingStage: invoice.fundingStage ?? "draft"
  }));
  save();
  render();
}

function save() {
  localStorage.setItem("factorlab-state", JSON.stringify(state));
}

function normalizeInvoice(invoice) {
  const rating = String(invoice.rating || "B").trim().toUpperCase();
  const status = String(invoice.status || "clean").trim().toLowerCase();
  const fundingStage = String(invoice.fundingStage || invoice.fundingstage || invoice.workflow || "draft").trim().toLowerCase();
  return {
    debtor: invoice.debtor || invoice.customer || "",
    invoice: invoice.invoice || invoice.invoice_no || invoice.number || "",
    amount: toNumber(invoice.amount),
    days: toNumber(invoice.days || invoice.days_to_pay || invoice.tenor, 45),
    rating: ["A", "B", "C", "D"].includes(rating) ? rating : "B",
    concentration: toNumber(invoice.concentration, 20),
    status: ["clean", "unverified", "disputed", "overdue"].includes(status) ? status : "clean",
    fundingStage: validFundingStages.includes(fundingStage) ? fundingStage : "draft"
  };
}

function load() {
  const stored = localStorage.getItem("factorlab-state");
  if (!stored) {
    state.invoices = structuredClone(sampleInvoices).map(normalizeInvoice);
    return;
  }
  try {
    const parsed = JSON.parse(stored);
    state.terms = { ...defaultTerms, ...(parsed.terms || {}) };
    state.invoices = Array.isArray(parsed.invoices) ? parsed.invoices.map(normalizeInvoice) : structuredClone(sampleInvoices).map(normalizeInvoice);
  } catch {
    state.invoices = structuredClone(sampleInvoices).map(normalizeInvoice);
  }
}

function exportCsv() {
  const rows = [
    ["debtor", "invoice", "amount", "days", "rating", "concentration", "status", "fundingStage", "eligible", "eligibilityReasons"],
    ...state.invoices.map((item) => [
      item.debtor,
      item.invoice,
      item.amount,
      item.days,
      item.rating,
      item.concentration,
      item.status,
      item.fundingStage,
      isEligible(item) ? "yes" : "no",
      invoiceEligibility(item).reasons.join("; ")
    ])
  ];
  const csv = rows
    .map((row) => row.map((cell) => `"${String(cell).replaceAll('"', '""')}"`).join(","))
    .join("\n");
  const blob = new Blob([csv], { type: "text/csv" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "factorlab-invoices.csv";
  link.click();
  URL.revokeObjectURL(url);
}

function importCsv(file) {
  const reader = new FileReader();
  reader.onload = () => {
    const rows = parseCsv(String(reader.result || ""));
    const [header, ...data] = rows;
    if (!header) return;
    const keys = header.map((key) => key.trim().toLowerCase());
    state.invoices = data.filter((row) => row.length > 1).map((row) => {
      const record = Object.fromEntries(keys.map((key, index) => [key, row[index]]));
      return normalizeInvoice(record);
    });
    save();
    render();
  };
  reader.readAsText(file);
}

function parseCsv(text) {
  const rows = [];
  let row = [];
  let cell = "";
  let quoted = false;
  for (let i = 0; i < text.length; i += 1) {
    const char = text[i];
    const next = text[i + 1];
    if (char === '"' && quoted && next === '"') {
      cell += '"';
      i += 1;
    } else if (char === '"') {
      quoted = !quoted;
    } else if (char === "," && !quoted) {
      row.push(cell);
      cell = "";
    } else if ((char === "\n" || char === "\r") && !quoted) {
      if (cell || row.length) rows.push([...row, cell]);
      row = [];
      cell = "";
      if (char === "\r" && next === "\n") i += 1;
    } else {
      cell += char;
    }
  }
  if (cell || row.length) rows.push([...row, cell]);
  return rows;
}

els.termsForm.addEventListener("input", syncTermsFromInputs);
els.termsForm.addEventListener("change", syncTermsFromInputs);
els.addInvoiceBtn.addEventListener("click", () => addInvoice());
els.submitEligibleBtn.addEventListener("click", () => {
  state.invoices = state.invoices.map((invoice) => {
    if (invoice.fundingStage === "draft" && isEligible(invoice)) return { ...invoice, fundingStage: "submitted" };
    return invoice;
  });
  save();
  render();
});
els.loadSampleBtn.addEventListener("click", () => {
  state.invoices = structuredClone(sampleInvoices).map(normalizeInvoice);
  save();
  render();
});
els.resetTermsBtn.addEventListener("click", () => {
  state.terms = { ...defaultTerms };
  save();
  render();
});
els.exportBtn.addEventListener("click", exportCsv);
els.csvInput.addEventListener("change", (event) => {
  const file = event.target.files?.[0];
  if (file) importCsv(file);
  event.target.value = "";
});
els.copyOfferBtn.addEventListener("click", async () => {
  await navigator.clipboard.writeText(els.offerText.textContent);
  els.copyOfferBtn.textContent = "Copied";
  window.setTimeout(() => {
    els.copyOfferBtn.textContent = "Copy";
  }, 1200);
});
document.querySelectorAll(".segment").forEach((button) => {
  button.addEventListener("click", () => {
    state.terms.model = button.dataset.model;
    save();
    render();
  });
});
window.addEventListener("resize", () => drawChart(calculate()));

load();
render();

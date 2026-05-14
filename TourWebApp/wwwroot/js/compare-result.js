(function () {
  "use strict";

  const STORAGE_KEY = "tour_compare_ids";

  // ── Root elements ──────────────────────────────────────────
  const matrix = document.getElementById("tourCompareTable");
  if (!matrix) return;

  const compareInsight    = document.getElementById("compareInsight");
  const sortPriceBtn      = document.getElementById("sortPriceBtn");
  const sortViewBtn       = document.getElementById("sortViewBtn");
  const toggleSameRowsBtn = document.getElementById("toggleSameRowsBtn");
  const copyLinkBtn       = document.getElementById("copyCompareLinkBtn");

  let hideSame  = false;
  let priceAsc  = true;
  let viewDesc  = true;

  // ── Helpers ────────────────────────────────────────────────

  /** All rows in the matrix */
  function getRows() {
    return Array.from(matrix.querySelectorAll(".cmp-matrix__row"));
  }

  /** Header tour-card elements (hold data-* attributes) */
  function getTourHeaders() {
    return Array.from(matrix.querySelectorAll(".cmp-tour-header"));
  }

  /** Ordered list of tour ids from current header order */
  function getCurrentIds() {
    return getTourHeaders().map(h => Number(h.dataset.tourId));
  }

  // ── Column reorder ─────────────────────────────────────────
  /**
   * Reorder tour columns in every row to match `orderedIds`.
   * Works on .cmp-matrix__col elements (all except the first label cell).
   */
  function applyColumnOrder(orderedIds) {
    getRows().forEach(row => {
      const cols = Array.from(row.querySelectorAll(".cmp-matrix__col, .cmp-tour-header"));
      if (cols.length === 0) return;

      // Build id→element map using header row for cells that don't carry tour-id
      // For the header row cols carry data-tour-id directly.
      // For data rows we match positionally via header order before reorder.
      const headerRow = matrix.querySelector(".cmp-matrix__row--header");
      const headerCols = Array.from(headerRow.querySelectorAll(".cmp-tour-header"));
      const posById = {};
      headerCols.forEach((h, i) => { posById[Number(h.dataset.tourId)] = i; });

      // Sort by orderedIds using position index
      const dataCols = Array.from(row.querySelectorAll(".cmp-matrix__col, .cmp-tour-header"));
      if (dataCols.length === 0) return;

      const sorted = orderedIds.map(id => dataCols[posById[id]]).filter(Boolean);
      const parent = dataCols[0].parentElement;
      sorted.forEach(el => parent.appendChild(el));
    });
  }

  // ── Best-value highlighting ────────────────────────────────
  function refreshBestHighlights() {
    const headerCols = getTourHeaders();

    getRows().forEach(row => {
      const kind = row.dataset.rowKind;
      if (kind !== "numeric-low" && kind !== "numeric-high") return;

      const cols = Array.from(row.querySelectorAll(".cmp-matrix__col"));
      cols.forEach(c => c.classList.remove("is-best"));

      const values = cols.map(c => Number(c.dataset.value ?? 0));
      const target = kind === "numeric-low"
        ? Math.min(...values)
        : Math.max(...values);

      cols.forEach(c => {
        if (Number(c.dataset.value ?? 0) === target) {
          // Don't highlight "0 seats" as best for numeric-high
          if (kind === "numeric-high" && target === 0) return;
          c.classList.add("is-best");
        }
      });
    });
  }

  // ── Same-row toggle ────────────────────────────────────────
  function applyHideSame() {
    getRows().forEach(row => {
      const kind = row.dataset.rowKind;
      if (kind === "header" || kind === "action") return;

      const vals = Array.from(row.querySelectorAll(".cmp-matrix__col"))
        .map(c => c.textContent.trim());
      const allSame = vals.length > 1 && vals.every(v => v === vals[0]);
      row.classList.toggle("is-hidden-same", hideSame && allSame);
    });
  }

  // ── Insight cards ──────────────────────────────────────────
  function renderInsights() {
    if (!compareInsight) return;

    const headers = getTourHeaders();
    if (headers.length === 0) return;

    // Resolve tour name: name text is in .cmp-tour-header__name inside the header col
    const tours = headers.map(h => ({
      id:    Number(h.dataset.tourId),
      name:  (h.querySelector(".cmp-tour-header__name")?.textContent ?? "").trim(),
      price: Number(h.dataset.price ?? 0),
      views: Number(h.dataset.views ?? 0),
      seats: Number(h.dataset.seats ?? 0),
    }));

    const minPrice = Math.min(...tours.map(t => t.price));
    const maxViews = Math.max(...tours.map(t => t.views));
    const maxSeats = Math.max(...tours.map(t => t.seats));

    const cheapest  = tours.filter(t => t.price === minPrice).map(t => t.name);
    const hottest   = tours.filter(t => t.views === maxViews).map(t => t.name);

    // Warnings: sold-out, nearly-sold-out, overpriced
    const warnings = tours.flatMap(t => {
      const w = [];
      if (t.seats <= 0)              w.push(`${t.name}: hết chỗ`);
      else if (t.seats <= 3)         w.push(`${t.name}: sắp hết chỗ`);
      if (t.price > minPrice * 1.6)  w.push(`${t.name}: giá cao hơn mặt bằng`);
      return w;
    });

    const spacious = tours.filter(t => t.seats === maxSeats && t.seats > 0).map(t => t.name);
    const slotText = warnings.length
      ? warnings.join("; ")
      : (spacious.length ? `${spacious.join(", ")} còn nhiều chỗ` : "Đang cập nhật");

    compareInsight.innerHTML = `
      <article class="cmp-insight-card">
        <p class="cmp-insight-card__label">Giá tốt nhất</p>
        <p class="cmp-insight-card__value">${cheapest.join(", ")}${cheapest.length > 1 ? " (đồng hạng)" : ""}</p>
      </article>
      <article class="cmp-insight-card">
        <p class="cmp-insight-card__label">Phổ biến nhất</p>
        <p class="cmp-insight-card__value">${hottest.join(", ")}${hottest.length > 1 ? " (đồng hạng)" : ""}</p>
      </article>
      <article class="cmp-insight-card${warnings.length ? " cmp-insight-card--warning" : ""}">
        <p class="cmp-insight-card__label">Tình trạng chỗ</p>
        <p class="cmp-insight-card__value">${slotText}</p>
      </article>
    `;
  }

  // ── Sort buttons ───────────────────────────────────────────
  sortPriceBtn?.addEventListener("click", () => {
    const ids = getTourHeaders()
      .map(h => ({ id: Number(h.dataset.tourId), price: Number(h.dataset.price ?? 0) }))
      .sort((a, b) => priceAsc ? a.price - b.price : b.price - a.price)
      .map(x => x.id);

    applyColumnOrder(ids);
    refreshBestHighlights();
    renderInsights();
    priceAsc = !priceAsc;
  });

  sortViewBtn?.addEventListener("click", () => {
    const ids = getTourHeaders()
      .map(h => ({ id: Number(h.dataset.tourId), views: Number(h.dataset.views ?? 0) }))
      .sort((a, b) => viewDesc ? b.views - a.views : a.views - b.views)
      .map(x => x.id);

    applyColumnOrder(ids);
    refreshBestHighlights();
    renderInsights();
    viewDesc = !viewDesc;
  });

  // ── Hide same rows ─────────────────────────────────────────
  toggleSameRowsBtn?.addEventListener("click", () => {
    hideSame = !hideSame;
    applyHideSame();
    toggleSameRowsBtn.textContent = hideSame ? "Hiện tất cả dòng" : "Ẩn dòng giống nhau";
  });

  // ── Copy link ──────────────────────────────────────────────
  copyLinkBtn?.addEventListener("click", async () => {
    const ids = getCurrentIds().filter(Boolean);
    const url = `${location.origin}/Tour/SoSanh?ids=${ids.join(",")}`;
    try {
      await navigator.clipboard.writeText(url);
      copyLinkBtn.textContent = "Đã copy ✓";
      setTimeout(() => { copyLinkBtn.textContent = "Copy link"; }, 1500);
    } catch {
      window.prompt("Copy link so sánh:", url);
    }
  });

  // ── Remove tour ────────────────────────────────────────────
  matrix.addEventListener("click", e => {
    const btn = e.target.closest(".remove-compare-item");
    if (!btn) return;

    const removeId = Number(btn.dataset.tourId);
    if (!removeId) return;

    const remaining = getCurrentIds().filter(id => id !== removeId);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(remaining));

    if (remaining.length < 2) {
      location.href = "/Tour/TatCa";
      return;
    }
    location.href = `/Tour/SoSanh?ids=${remaining.join(",")}`;
  });

  // ── Init ───────────────────────────────────────────────────
  refreshBestHighlights();
  renderInsights();
})();

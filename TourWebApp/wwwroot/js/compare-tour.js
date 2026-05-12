(function () {
  const STORAGE_KEY = "tour_compare_ids";
  const MAX_ITEMS = 4;

  const bar = document.getElementById("compareBar");
  const countEl = document.getElementById("compareCount");
  const compareBtn = document.getElementById("compareNowBtn");
  const clearBtn = document.getElementById("clearCompareBtn");
  const toggleButtons = Array.from(document.querySelectorAll(".compare-toggle-btn"));

  if (!bar || !countEl || !compareBtn || !clearBtn || toggleButtons.length === 0) return;

  function getIds() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      const parsed = raw ? JSON.parse(raw) : [];
      return Array.isArray(parsed) ? parsed.filter(Number.isInteger) : [];
    } catch {
      return [];
    }
  }

  function saveIds(ids) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(ids.slice(0, MAX_ITEMS)));
  }

  function updateUI() {
    const ids = getIds();
    bar.hidden = ids.length === 0;
    countEl.textContent = `${ids.length}/${MAX_ITEMS} tour`;
    compareBtn.disabled = ids.length < 2;

    toggleButtons.forEach((btn) => {
      const id = Number(btn.dataset.tourId);
      const selected = ids.includes(id);
      btn.classList.toggle("active", selected);
      btn.textContent = selected ? "Đã chọn" : "Chọn so sánh";
    });
  }

  toggleButtons.forEach((btn) => {
    btn.addEventListener("click", function () {
      const id = Number(this.dataset.tourId);
      if (!id) return;

      const ids = getIds();
      const idx = ids.indexOf(id);
      if (idx >= 0) {
        ids.splice(idx, 1);
      } else {
        if (ids.length >= MAX_ITEMS) {
          alert("Bạn chỉ có thể so sánh tối đa 4 tour.");
          return;
        }
        ids.push(id);
      }
      saveIds(ids);
      updateUI();
    });
  });

  compareBtn.addEventListener("click", function () {
    const ids = getIds();
    if (ids.length < 2) return;
    window.location.href = `/Tour/SoSanh?ids=${ids.join(",")}`;
  });

  clearBtn.addEventListener("click", function () {
    saveIds([]);
    updateUI();
  });

  updateUI();
})();

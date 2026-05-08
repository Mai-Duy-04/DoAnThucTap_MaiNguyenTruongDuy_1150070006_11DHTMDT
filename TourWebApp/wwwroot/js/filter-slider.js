const rangeMin = document.querySelector(".range-min");
const rangeMax = document.querySelector(".range-max");
const track = document.querySelector(".slider-track");

const inputMin = document.getElementById("giaminInput");
const inputMax = document.getElementById("giamaxInput");
const filterForm = document.getElementById("filterForm");

function onlyDigits(value) {
    return (value || "").toString().replace(/\D/g, "");
}

function asNumber(value, fallback = 0) {
    const n = parseInt(value, 10);
    return Number.isNaN(n) ? fallback : n;
}

function updateSlider() {
    if (!rangeMin || !rangeMax || !track || !inputMin || !inputMax) return;

    let minVal = asNumber(rangeMin.value, 0);
    let maxVal = asNumber(rangeMax.value, asNumber(rangeMax.max, 20000000));

    if (minVal > maxVal) minVal = maxVal;
    if (maxVal < minVal) maxVal = minVal;

    rangeMin.value = String(minVal);
    rangeMax.value = String(maxVal);

    inputMin.value = minVal.toLocaleString("vi-VN") + " d";
    inputMax.value = maxVal.toLocaleString("vi-VN") + " d";

    const maxLimit = asNumber(rangeMax.max, 20000000);
    const minPercent = (minVal / maxLimit) * 100;
    const maxPercent = (maxVal / maxLimit) * 100;

    track.style.background = `linear-gradient(
        to right,
        #ddd ${minPercent}%,
        #0d6efd ${minPercent}%,
        #0d6efd ${maxPercent}%,
        #ddd ${maxPercent}%
    )`;
}

function initPriceRange() {
    if (!rangeMin || !rangeMax || !inputMin || !inputMax) return;

    // Khoi tao tu query (neu co)
    const queryMin = onlyDigits(inputMin.value);
    const queryMax = onlyDigits(inputMax.value);

    if (queryMin) rangeMin.value = queryMin;
    if (queryMax) rangeMax.value = queryMax;

    rangeMin.addEventListener("input", () => {
        if (+rangeMin.value > +rangeMax.value) rangeMin.value = rangeMax.value;
        updateSlider();
    });

    rangeMax.addEventListener("input", () => {
        if (+rangeMax.value < +rangeMin.value) rangeMax.value = rangeMin.value;
        updateSlider();
    });

    updateSlider();

    if (filterForm) {
        filterForm.addEventListener("submit", () => {
            inputMin.value = onlyDigits(inputMin.value);
            inputMax.value = onlyDigits(inputMax.value);
        });
    }
}

function initAutoCompleteDiaDiem() {
    const pairs = [
        { input: document.getElementById("diadiemInput"), box: document.getElementById("goiYBox") },
        { input: document.getElementById("diadiemInputTop"), box: document.getElementById("goiYBoxTop") }
    ].filter(p => p.input && p.box);

    if (!pairs.length) return;

    pairs.forEach(({ input, box }) => {
        input.addEventListener("input", function () {
            const keyword = input.value.trim();

            if (keyword.length < 1) {
                box.style.display = "none";
                return;
            }

            fetch(`/Tour/GoiyDiaDiem?keyword=${encodeURIComponent(keyword)}`)
                .then(res => res.json())
                .then(data => {
                    if (!data || data.length === 0) {
                        box.style.display = "none";
                        return;
                    }

                    box.innerHTML = "";

                    data.forEach(item => {
                        const div = document.createElement("div");
                        div.classList.add("autocomplete-item");
                        div.textContent = item;

                        div.onclick = function () {
                            input.value = item;
                            box.style.display = "none";
                        };

                        box.appendChild(div);
                    });

                    box.style.display = "block";
                })
                .catch(() => {
                    box.style.display = "none";
                });
        });

        document.addEventListener("click", function (e) {
            if (!input.contains(e.target) && !box.contains(e.target)) {
                box.style.display = "none";
            }
        });
    });
}

function initCustomSelects() {
    const selects = document.querySelectorAll(
        "#quickFilterForm select[name='phuongtien'], #quickFilterForm select[name='idLoaiTour'], #filterForm select[name='phuongtien'], #filterForm select[name='idLoaiTour']"
    );

    if (!selects.length) return;

    let openDropdown = null;

    function closeDropdown(dropdown) {
        if (!dropdown) return;
        dropdown.classList.remove("is-open");
        const panel = dropdown.querySelector(".tour-custom-select__menu");
        if (panel) panel.hidden = true;
        openDropdown = openDropdown === dropdown ? null : openDropdown;
    }

    function openDropdownMenu(dropdown) {
        if (!dropdown) return;
        if (openDropdown && openDropdown !== dropdown) closeDropdown(openDropdown);
        dropdown.classList.add("is-open");
        const panel = dropdown.querySelector(".tour-custom-select__menu");
        if (panel) panel.hidden = false;
        openDropdown = dropdown;
    }

    selects.forEach((select) => {
        if (select.dataset.customized === "1") return;
        select.dataset.customized = "1";
        select.classList.add("tour-native-select");

        const wrapper = document.createElement("div");
        wrapper.className = "tour-custom-select";

        const button = document.createElement("button");
        button.type = "button";
        button.className = "tour-custom-select__trigger";
        button.setAttribute("aria-haspopup", "listbox");
        button.setAttribute("aria-expanded", "false");

        const label = document.createElement("span");
        label.className = "tour-custom-select__label";
        button.appendChild(label);

        const arrow = document.createElement("span");
        arrow.className = "tour-custom-select__arrow";
        button.appendChild(arrow);

        const menu = document.createElement("div");
        menu.className = "tour-custom-select__menu";
        menu.setAttribute("role", "listbox");
        menu.hidden = true;

        function syncFromSelect() {
            const selectedOption = select.options[select.selectedIndex];
            label.textContent = selectedOption ? selectedOption.textContent : "Chọn";

            menu.querySelectorAll(".tour-custom-select__option").forEach((optEl) => {
                const isActive = optEl.dataset.value === select.value;
                optEl.classList.toggle("is-active", isActive);
                optEl.setAttribute("aria-selected", isActive ? "true" : "false");
            });
        }

        Array.from(select.options).forEach((opt) => {
            const optEl = document.createElement("button");
            optEl.type = "button";
            optEl.className = "tour-custom-select__option";
            optEl.dataset.value = opt.value;
            optEl.setAttribute("role", "option");
            optEl.textContent = opt.textContent;

            optEl.addEventListener("click", () => {
                if (select.value !== opt.value) {
                    select.value = opt.value;
                    select.dispatchEvent(new Event("change", { bubbles: true }));
                }
                syncFromSelect();
                closeDropdown(wrapper);
            });

            menu.appendChild(optEl);
        });

        button.addEventListener("click", () => {
            const isOpen = wrapper.classList.contains("is-open");
            if (isOpen) {
                closeDropdown(wrapper);
                button.setAttribute("aria-expanded", "false");
            } else {
                openDropdownMenu(wrapper);
                button.setAttribute("aria-expanded", "true");
            }
        });

        button.addEventListener("keydown", (e) => {
            if (e.key === "ArrowDown" || e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                openDropdownMenu(wrapper);
                button.setAttribute("aria-expanded", "true");
            }
            if (e.key === "Escape") {
                closeDropdown(wrapper);
                button.setAttribute("aria-expanded", "false");
            }
        });

        select.addEventListener("change", syncFromSelect);

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(button);
        wrapper.appendChild(menu);
        wrapper.appendChild(select);

        syncFromSelect();
    });

    document.addEventListener("click", (e) => {
        if (!openDropdown) return;
        if (!openDropdown.contains(e.target)) {
            closeDropdown(openDropdown);
        }
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && openDropdown) {
            closeDropdown(openDropdown);
        }
    });
}

initPriceRange();
initAutoCompleteDiaDiem();
initCustomSelects();

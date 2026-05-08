document.addEventListener("DOMContentLoaded", function () {
    initTabs();
    initScheduleSelection();
    initScrollButtons();
    initGallery();
    initCalculator();
    initSortComments();
    initAutoGrow();
});

function initTabs() {
    const tabs = document.querySelectorAll(".tour-tab-nav li");
    if (!tabs.length) return;

    function setActiveTab(targetId) {
        tabs.forEach(function (tab) {
            tab.classList.toggle("active", tab.getAttribute("data-target") === targetId);
        });
    }

    const firstActive = document.querySelector(".tour-tab-nav li.active");
    const initialTargetId =
        firstActive?.getAttribute("data-target") || tabs[0].getAttribute("data-target");

    if (initialTargetId) {
        setActiveTab(initialTargetId);
    }

    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            const targetId = tab.getAttribute("data-target");
            if (!targetId) return;

            setActiveTab(targetId);

            const target = document.getElementById(targetId);
            if (target) {
                window.scrollTo({
                    top: target.getBoundingClientRect().top + window.scrollY - 120,
                    behavior: "smooth"
                });
            }
        });
    });
}

function initScheduleSelection() {
    const rows = Array.from(document.querySelectorAll(".lich-row"));
    if (!rows.length) return;

    const selectedLichInput = document.getElementById("selectedLich");
    const soNgay = parseInt(document.getElementById("songay")?.value || "0", 10);

    function updateKetThuc(ngayKhoiHanh) {
        const ketThucEl = document.getElementById("thoigian_ketthuc");
        if (!ketThucEl) return;

        if (!ngayKhoiHanh || soNgay <= 0) {
            ketThucEl.textContent = "";
            return;
        }

        const parts = ngayKhoiHanh.split("/");
        if (parts.length !== 3) {
            ketThucEl.textContent = "";
            return;
        }

        const start = new Date(Number(parts[2]), Number(parts[1]) - 1, Number(parts[0]));
        if (Number.isNaN(start.getTime())) {
            ketThucEl.textContent = "";
            return;
        }

        start.setDate(start.getDate() + soNgay - 1);
        ketThucEl.textContent = start.toLocaleDateString("vi-VN");
    }

    function applyRow(row) {
        rows.forEach(function (item) {
            item.classList.remove("lich-selected");
        });
        row.classList.add("lich-selected");

        if (selectedLichInput) {
            selectedLichInput.value = row.dataset.idlich || "";
        }

        const ngay = row.dataset.ngay || "";
        const gio = row.dataset.gio || "--";
        const trangThai = row.dataset.trangthai || "--";
        const soCho = row.dataset.socho || "--";

        const ngayEl = document.getElementById("thoigian_ngay");
        const gioEl = document.getElementById("thoigian_gio");
        const trangThaiEl = document.getElementById("thoigian_trangthai");
        const soChoEl = document.getElementById("thoigian_socho");

        if (ngayEl) ngayEl.textContent = ngay;
        if (gioEl) gioEl.textContent = gio;
        if (trangThaiEl) trangThaiEl.textContent = trangThai;
        if (soChoEl) soChoEl.textContent = soCho;

        updateKetThuc(ngay);
    }

    rows.forEach(function (row) {
        row.addEventListener("click", function () {
            applyRow(row);
        });
    });

    const firstSelected = rows.find(function (row) {
        return row.classList.contains("lich-selected");
    }) || rows[0];

    if (firstSelected) {
        applyRow(firstSelected);
    }
}

function initScrollButtons() {
    const buttons = document.querySelectorAll(".scroll-btn");
    if (!buttons.length) return;

    buttons.forEach(function (button) {
        button.addEventListener("click", function () {
            const targetId = button.getAttribute("data-target");
            if (!targetId) return;

            const tab = document.querySelector(".tour-tab-nav li[data-target='" + targetId + "']");
            if (tab) {
                tab.click();
                return;
            }

            const section = document.getElementById(targetId);
            if (section) {
                window.scrollTo({
                    top: section.getBoundingClientRect().top + window.scrollY - 120,
                    behavior: "smooth"
                });
            }
        });
    });
}

function initGallery() {
    const thumbs = document.querySelectorAll(".thumb-img");
    if (!thumbs.length) return;

    thumbs.forEach(function (thumb) {
        thumb.addEventListener("click", function () {
            setActiveThumb(thumb);
        });
    });
}

function setActiveThumb(activeThumb) {
    const thumbs = document.querySelectorAll(".thumb-img");
    thumbs.forEach(function (thumb) {
        thumb.classList.remove("is-active");
    });
    activeThumb.classList.add("is-active");
}

function initCalculator() {
    const calcBox = document.querySelector(".tour-calc-box");
    if (!calcBox) return;

    const adultPrice = parseFloat(calcBox.dataset.adult || "0") || 0;
    const childPrice = parseFloat(calcBox.dataset.child || "0") || 0;
    const babyPrice = parseFloat(calcBox.dataset.baby || "0") || 0;

    const adultInput = document.getElementById("adultQty");
    const childInput = document.getElementById("childQty");
    const babyInput = document.getElementById("babyQty");
    const totalAmountEl = document.getElementById("totalAmount");
    const totalGuestEl = document.getElementById("totalGuest");

    if (!adultInput || !childInput || !babyInput) return;

    function safeToInt(value) {
        const parsed = parseInt(value || "0", 10);
        return Number.isNaN(parsed) ? 0 : Math.max(parsed, 0);
    }

    function formatMoney(value) {
        return value.toLocaleString("vi-VN") + " đ";
    }

    function recalc() {
        const adult = safeToInt(adultInput.value);
        const child = safeToInt(childInput.value);
        const baby = safeToInt(babyInput.value);

        adultInput.value = String(adult);
        childInput.value = String(child);
        babyInput.value = String(baby);

        const total = adult * adultPrice + child * childPrice + baby * babyPrice;
        const guest = adult + child + baby;

        if (totalAmountEl) totalAmountEl.textContent = formatMoney(total);
        if (totalGuestEl) totalGuestEl.textContent = String(guest);
    }

    function bindQty(minusId, inputEl, plusId) {
        const minusBtn = document.getElementById(minusId);
        const plusBtn = document.getElementById(plusId);

        minusBtn?.addEventListener("click", function (event) {
            event.preventDefault();
            inputEl.value = String(Math.max(safeToInt(inputEl.value) - 1, 0));
            recalc();
        });

        plusBtn?.addEventListener("click", function (event) {
            event.preventDefault();
            inputEl.value = String(safeToInt(inputEl.value) + 1);
            recalc();
        });

        inputEl.addEventListener("change", recalc);
        inputEl.addEventListener("input", recalc);
    }

    bindQty("adultMinus", adultInput, "adultPlus");
    bindQty("childMinus", childInput, "childPlus");
    bindQty("babyMinus", babyInput, "babyPlus");

    recalc();
}

function initSortComments() {
    const sort = document.getElementById("sortComment");
    if (!sort) return;

    sort.addEventListener("change", function () {
        const url = new URL(window.location.href);
        url.searchParams.set("sortCmt", sort.value);
        window.location.href = url.toString();
    });
}

function initAutoGrow() {
    const textareas = document.querySelectorAll(".auto-grow");
    textareas.forEach(function (el) {
        el.addEventListener("input", function () {
            el.style.height = "auto";
            el.style.height = el.scrollHeight + "px";
        });
    });
}

function goToBooking() {
    const selectedLichInput = document.getElementById("selectedLich");
    const calcBox = document.querySelector(".tour-calc-box");
    if (!selectedLichInput || !calcBox) return;

    const idLich = selectedLichInput.value;
    const idTour = calcBox.dataset.idtour;
    const user = calcBox.dataset.user;

    const adult = parseInt(document.getElementById("adultQty")?.value || "0", 10) || 0;
    const child = parseInt(document.getElementById("childQty")?.value || "0", 10) || 0;
    const baby = parseInt(document.getElementById("babyQty")?.value || "0", 10) || 0;

    const adultPrice = parseInt(calcBox.dataset.adult || "0", 10) || 0;
    const childPrice = parseInt(calcBox.dataset.child || "0", 10) || 0;
    const babyPrice = parseInt(calcBox.dataset.baby || "0", 10) || 0;

    const total = adult * adultPrice + child * childPrice + baby * babyPrice;

    if (!idLich) {
        alert("Vui lòng chọn lịch khởi hành!");
        return;
    }

    const bookingQuery =
        "idTour=" + encodeURIComponent(idTour || "") +
        "&idLich=" + encodeURIComponent(idLich) +
        "&adult=" + encodeURIComponent(String(adult)) +
        "&child=" + encodeURIComponent(String(child)) +
        "&baby=" + encodeURIComponent(String(baby)) +
        "&total=" + encodeURIComponent(String(total));

    const bookingUrl = "/DatTour/NhapThongTin?" + bookingQuery;

    if (!user) {
        const loginBtn = document.getElementById("btnGoLogin");
        if (loginBtn) {
            loginBtn.href = "/TaiKhoan/DangNhap?returnUrl=" + encodeURIComponent(bookingUrl);
        }

        if (window.bootstrap) {
            const modalEl = document.getElementById("loginWarningModal");
            if (modalEl) {
                const loginPopup = new bootstrap.Modal(modalEl);
                loginPopup.show();
                return;
            }
        }

        window.location.href = "/TaiKhoan/DangNhap?returnUrl=" + encodeURIComponent(bookingUrl);
        return;
    }

    window.location.href = bookingUrl;
}

function changeMainImage(src) {
    const mainImg = document.getElementById("main-img");
    if (mainImg) {
        mainImg.src = src;
    }

    const thumbs = document.querySelectorAll(".thumb-img");
    thumbs.forEach(function (thumb) {
        const sameSource =
            thumb.currentSrc === src ||
            thumb.src === src ||
            thumb.getAttribute("src") === src;
        thumb.classList.toggle("is-active", sameSource);
    });
}

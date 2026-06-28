document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".counter").forEach(counter => {
        const target = Number(counter.dataset.count || 0);
        let current = 0;

        const duration = 950;
        const frameRate = 16;
        const totalFrames = Math.round(duration / frameRate);
        const increment = target / totalFrames;

        const run = setInterval(() => {
            current += increment;

            if (current >= target) {
                counter.textContent = target.toLocaleString("vi-VN");
                clearInterval(run);
            } else {
                counter.textContent = Math.floor(current).toLocaleString("vi-VN");
            }
        }, frameRate);
    });

    const moneyFormat = value => {
        return Number(value || 0).toLocaleString("vi-VN") + " đ";
    };

    const datePicker = document.getElementById("singleDatePicker");

    if (datePicker && typeof flatpickr !== "undefined") {
        flatpickr("#singleDatePicker", {
            mode: "single",
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d/m/Y",
            defaultDate: window.selectedDate || new Date(),
            disableMobile: true,
            onChange: function (selectedDates, dateStr, instance) {
                if (selectedDates.length === 1) {
                    const selectedDateStr = instance.formatDate(selectedDates[0], "Y-m-d");

                    const wrapper = document.querySelector(".dash-date-picker-wrapper");
                    if (wrapper) {
                        wrapper.style.opacity = "0.65";
                        wrapper.style.pointerEvents = "none";
                    }

                    window.location.href = `/QuanTri/Dashboard?period=day&selectedDate=${selectedDateStr}`;
                }
            }
        });
    }

    if (typeof Chart === "undefined") return;

    Chart.defaults.font.family = "'Plus Jakarta Sans', sans-serif";
    Chart.defaults.color = "#64748b";

    const revenueCanvas = document.getElementById("chartDoanhThu");

    if (revenueCanvas) {
        const ctx = revenueCanvas.getContext("2d");

        const gradient = ctx.createLinearGradient(0, 0, 0, 360);
        gradient.addColorStop(0, "rgba(102, 88, 246, 0.28)");
        gradient.addColorStop(0.55, "rgba(102, 88, 246, 0.10)");
        gradient.addColorStop(1, "rgba(102, 88, 246, 0.00)");

        new Chart(revenueCanvas, {
            type: "line",
            data: {
                labels: window.labelDoanhThu || [],
                datasets: [{
                    label: "Doanh thu",
                    data: window.dataDoanhThu || [],
                    borderColor: "#6658f6",
                    borderWidth: 4,
                    backgroundColor: gradient,
                    fill: true,
                    tension: 0.45,
                    pointRadius: 0,
                    pointHoverRadius: 6,
                    pointHoverBackgroundColor: "#6658f6",
                    pointHoverBorderColor: "#ffffff",
                    pointHoverBorderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: "index",
                    intersect: false
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        enabled: true,
                        backgroundColor: "#0f172a",
                        titleColor: "#ffffff",
                        bodyColor: "#f8fafc",
                        padding: 13,
                        cornerRadius: 12,
                        displayColors: false,
                        callbacks: {
                            label: context => "Doanh thu: " + moneyFormat(context.raw)
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false,
                            drawBorder: false
                        },
                        ticks: {
                            color: "#94a3b8",
                            font: {
                                size: 12,
                                weight: "600"
                            }
                        }
                    },
                    y: {
                        beginAtZero: true,
                        border: {
                            display: false
                        },
                        ticks: {
                            color: "#94a3b8",
                            font: {
                                size: 12,
                                weight: "600"
                            },
                            callback: value => {
                                if (value >= 1000000000) return (value / 1000000000).toFixed(1) + " tỷ";
                                if (value >= 1000000) return (value / 1000000).toFixed(0) + "tr";
                                if (value >= 1000) return (value / 1000).toFixed(0) + "k";
                                return value;
                            }
                        },
                        grid: {
                            color: "#e8eef6",
                            borderDash: [6, 6],
                            drawTicks: false
                        }
                    }
                }
            }
        });
    }

    const customerCanvas = document.getElementById("chartKhach");

    if (customerCanvas) {
        const ctx = customerCanvas.getContext("2d");

        const barGradient = ctx.createLinearGradient(0, 0, 0, 320);
        barGradient.addColorStop(0, "#38bdf8");
        barGradient.addColorStop(1, "rgba(56, 189, 248, 0.25)");

        new Chart(customerCanvas, {
            type: "bar",
            data: {
                labels: window.labelKhach || [],
                datasets: [{
                    label: "Khách đặt",
                    data: window.dataKhach || [],
                    backgroundColor: barGradient,
                    hoverBackgroundColor: "#0ea5e9",
                    borderRadius: 12,
                    borderSkipped: false,
                    maxBarThickness: 30,
                    barPercentage: 0.62,
                    categoryPercentage: 0.72
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: "#0f172a",
                        titleColor: "#ffffff",
                        bodyColor: "#f8fafc",
                        padding: 13,
                        cornerRadius: 12,
                        displayColors: false,
                        callbacks: {
                            label: context => {
                                return "Khách đặt: " + Number(context.raw || 0).toLocaleString("vi-VN") + " khách";
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false,
                            drawBorder: false
                        },
                        ticks: {
                            color: "#94a3b8",
                            font: {
                                size: 12,
                                weight: "600"
                            }
                        }
                    },
                    y: {
                        beginAtZero: true,
                        border: {
                            display: false
                        },
                        ticks: {
                            precision: 0,
                            color: "#94a3b8",
                            font: {
                                size: 12,
                                weight: "600"
                            }
                        },
                        grid: {
                            color: "#e8eef6",
                            borderDash: [6, 6],
                            drawTicks: false
                        }
                    }
                }
            }
        });
    }
});
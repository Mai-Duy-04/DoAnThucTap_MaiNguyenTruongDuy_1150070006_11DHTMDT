document.querySelectorAll(".counter").forEach(counter => {
    const target = Number(counter.dataset.count || 0);
    let current = 0;
    const step = Math.max(1, Math.ceil(target / 60));

    const run = setInterval(() => {
        current += step;

        if (current >= target) {
            counter.textContent = target.toLocaleString("vi-VN");
            clearInterval(run);
        } else {
            counter.textContent = current.toLocaleString("vi-VN");
        }
    }, 18);
});

const moneyFormat = value => {
    return Number(value || 0).toLocaleString("vi-VN") + " đ";
};

const chartDoanhThu = document.getElementById("chartDoanhThu");

if (chartDoanhThu) {
    new Chart(chartDoanhThu, {
        type: "line",
        data: {
            labels: window.labelDoanhThu || [],
            datasets: [{
                label: "Doanh thu",
                data: window.dataDoanhThu || [],
                borderColor: "#4f46e5",
                backgroundColor: "rgba(79, 70, 229, 0.12)",
                fill: true,
                tension: 0.38,
                pointRadius: 4,
                pointHoverRadius: 6,
                pointBackgroundColor: "#4f46e5",
                pointBorderColor: "#ffffff",
                pointBorderWidth: 2
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
                    callbacks: {
                        label: ctx => "Doanh thu: " + moneyFormat(ctx.raw)
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: "#6b7280"
                    }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        color: "#6b7280",
                        callback: value => moneyFormat(value)
                    },
                    grid: {
                        color: "rgba(148, 163, 184, 0.22)"
                    }
                }
            }
        }
    });
}

const chartKhach = document.getElementById("chartKhach");

if (chartKhach) {
    new Chart(chartKhach, {
        type: "bar",
        data: {
            labels: window.labelKhach || [],
            datasets: [{
                label: "Khách đặt",
                data: window.dataKhach || [],
                backgroundColor: "#38bdf8",
                borderRadius: 12,
                maxBarThickness: 36
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
                    callbacks: {
                        label: ctx => "Khách đặt: " + Number(ctx.raw || 0).toLocaleString("vi-VN")
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: "#6b7280"
                    }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0,
                        color: "#6b7280"
                    },
                    grid: {
                        color: "rgba(148, 163, 184, 0.22)"
                    }
                }
            }
        }
    });
}
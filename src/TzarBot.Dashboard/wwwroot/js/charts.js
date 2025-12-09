// TzarBot Dashboard Charts
// Uses Chart.js for rendering

window.dashboardCharts = {
    charts: {},

    // Initialize fitness chart with best/avg/worst lines
    initFitnessChart: function (canvasId, data) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        // Destroy existing chart if any
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels || [],
                datasets: [
                    {
                        label: 'Best',
                        data: data.best || [],
                        borderColor: '#22c55e',
                        backgroundColor: 'rgba(34, 197, 94, 0.1)',
                        borderWidth: 2,
                        fill: false,
                        tension: 0.3,
                        pointRadius: 0,
                        pointHoverRadius: 4
                    },
                    {
                        label: 'Average',
                        data: data.average || [],
                        borderColor: '#6366f1',
                        backgroundColor: 'rgba(99, 102, 241, 0.1)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.3,
                        pointRadius: 0,
                        pointHoverRadius: 4
                    },
                    {
                        label: 'Worst',
                        data: data.worst || [],
                        borderColor: '#ef4444',
                        backgroundColor: 'rgba(239, 68, 68, 0.1)',
                        borderWidth: 1,
                        borderDash: [5, 5],
                        fill: false,
                        tension: 0.3,
                        pointRadius: 0,
                        pointHoverRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(17, 17, 27, 0.9)',
                        titleColor: '#f8fafc',
                        bodyColor: '#94a3b8',
                        borderColor: 'rgba(255, 255, 255, 0.1)',
                        borderWidth: 1,
                        padding: 12,
                        displayColors: true
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: 'Generation',
                            color: '#64748b'
                        },
                        grid: {
                            color: 'rgba(255, 255, 255, 0.05)'
                        },
                        ticks: {
                            color: '#64748b'
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Fitness',
                            color: '#64748b'
                        },
                        grid: {
                            color: 'rgba(255, 255, 255, 0.05)'
                        },
                        ticks: {
                            color: '#64748b'
                        },
                        beginAtZero: true
                    }
                }
            }
        });
    },

    // Add a single data point to fitness chart
    addDataPoint: function (canvasId, point) {
        const chart = this.charts[canvasId];
        if (!chart) return;

        chart.data.labels.push(point.generation);
        chart.data.datasets[0].data.push(point.best);
        chart.data.datasets[1].data.push(point.average);
        chart.data.datasets[2].data.push(point.worst);

        // Keep last 100 points for performance
        if (chart.data.labels.length > 100) {
            chart.data.labels.shift();
            chart.data.datasets.forEach(ds => ds.data.shift());
        }

        chart.update('none');
    },

    // Update entire chart data
    updateChart: function (canvasId, data) {
        const chart = this.charts[canvasId];
        if (!chart) return;

        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.best;
        chart.data.datasets[1].data = data.average;
        chart.data.datasets[2].data = data.worst;
        chart.update();
    },

    // Initialize win rate chart
    initWinRateChart: function (canvasId, data) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels || [],
                datasets: [{
                    label: 'Win Rate %',
                    data: data.values || [],
                    borderColor: '#f59e0b',
                    backgroundColor: 'rgba(245, 158, 11, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointRadius: 0,
                    pointHoverRadius: 4
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
                        backgroundColor: 'rgba(17, 17, 27, 0.9)',
                        titleColor: '#f8fafc',
                        bodyColor: '#94a3b8',
                        callbacks: {
                            label: function (context) {
                                return `Win Rate: ${context.parsed.y.toFixed(1)}%`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: 'Generation',
                            color: '#64748b'
                        },
                        grid: {
                            color: 'rgba(255, 255, 255, 0.05)'
                        },
                        ticks: {
                            color: '#64748b'
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Win Rate %',
                            color: '#64748b'
                        },
                        grid: {
                            color: 'rgba(255, 255, 255, 0.05)'
                        },
                        ticks: {
                            color: '#64748b'
                        },
                        min: 0,
                        max: 100
                    }
                }
            }
        });
    },

    // Add win rate data point
    addWinRatePoint: function (canvasId, point) {
        const chart = this.charts[canvasId];
        if (!chart) return;

        chart.data.labels.push(point.generation);
        chart.data.datasets[0].data.push(point.value);

        if (chart.data.labels.length > 100) {
            chart.data.labels.shift();
            chart.data.datasets[0].data.shift();
        }

        chart.update('none');
    },

    // Initialize fitness distribution bar chart
    initDistributionChart: function (canvasId, data) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels || [],
                datasets: [{
                    label: 'Genomes',
                    data: data.values || [],
                    backgroundColor: 'rgba(99, 102, 241, 0.6)',
                    borderColor: '#6366f1',
                    borderWidth: 1,
                    borderRadius: 4
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
                        backgroundColor: 'rgba(17, 17, 27, 0.9)',
                        titleColor: '#f8fafc',
                        bodyColor: '#94a3b8'
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: 'Fitness Range',
                            color: '#64748b'
                        },
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#64748b',
                            maxRotation: 45
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Count',
                            color: '#64748b'
                        },
                        grid: {
                            color: 'rgba(255, 255, 255, 0.05)'
                        },
                        ticks: {
                            color: '#64748b',
                            stepSize: 1
                        },
                        beginAtZero: true
                    }
                }
            }
        });
    },

    // Update distribution chart
    updateDistributionChart: function (canvasId, data) {
        const chart = this.charts[canvasId];
        if (!chart) return;

        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.values;
        chart.update();
    },

    // Destroy a chart
    destroyChart: function (canvasId) {
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
            delete this.charts[canvasId];
        }
    }
};

/* =============================================================================
   admin-charts.js — CHARTS ON THE ADMIN STATISTICS PAGES
   =============================================================================
   Loaded only by Views/AdminStatistics/RecipeStats.cshtml and UserStats.cshtml,
   after the Chart.js library (https://www.chartjs.org).

   How a chart gets on the page:
     1. The view contains an empty <canvas id="..."> — the drawing surface.
     2. The view contains the numbers as JSON text in <script id="stats-data">.
     3. This file reads the numbers and calls  new Chart(canvas, settings).

   Each page has different canvases, so every function below first checks
   that its canvas exists and does nothing otherwise.
   ============================================================================= */

document.addEventListener('DOMContentLoaded', function () {

    // --- Read the numbers the server put on the page ---------------------------
    const dataElement = document.getElementById('stats-data');
    if (!dataElement) {
        return; // not a statistics page
    }
    const stats = JSON.parse(dataElement.textContent);

    // --- Take the colors from the design tokens (wwwroot/css/tokens.css) --------
    // so the charts always match the rest of the site.
    const styles = getComputedStyle(document.documentElement);
    function token(name) {
        return styles.getPropertyValue(name).trim();
    }
    const colors = {
        primary: token('--color-primary'),
        accent: token('--color-accent'),
        danger: token('--color-danger'),
        text: token('--color-text')
    };
    // Slices of the pie chart: our brand colors first, then a few extra tones.
    const sliceColors = [colors.danger, colors.accent, colors.primary, colors.text, '#A8DADC', '#E9C46A', '#8AB17D'];

    // Settings shared by all charts.
    Chart.defaults.font.family = token('--font-body');
    Chart.defaults.color = colors.text;
    const sharedOptions = {
        responsive: true,             // redraw when the window size changes
        maintainAspectRatio: false    // fill the .chart-box (its height is set in pages.css)
    };

    // Options for bar charts: start the vertical axis at 0 and count in whole numbers.
    const barOptions = Object.assign({}, sharedOptions, {
        scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
    });

    /* -----------------------------------------------------------------------
       RecipeStats page
       ----------------------------------------------------------------------- */

    // Pie chart: how many recipes each category has.
    const categoryCanvas = document.getElementById('categoryChart');
    if (categoryCanvas) {
        new Chart(categoryCanvas, {
            type: 'pie',
            data: {
                labels: stats.categoryLabels,
                datasets: [{ data: stats.categoryValues, backgroundColor: sliceColors }]
            },
            options: Object.assign({}, sharedOptions, {
                // Clicking a slice opens the recipe list filtered by that category.
                onClick: function (event, clickedElements) {
                    if (clickedElements.length === 0) {
                        return; // the click was outside the slices
                    }
                    const categoryName = stats.categoryLabels[clickedElements[0].index];
                    const categoryId = stats.categoryIds[categoryName];
                    if (categoryId !== undefined) {
                        // data-recipes-url on the canvas holds the address of the recipe list.
                        window.location.href = categoryCanvas.dataset.recipesUrl + '?SelectedCategoryId=' + categoryId;
                    }
                }
            })
        });
    }

    // Bar chart: at which hour of the day recipes are published.
    const hourCanvas = document.getElementById('hourChart');
    if (hourCanvas) {
        new Chart(hourCanvas, {
            type: 'bar',
            data: {
                labels: stats.hourLabels,
                datasets: [{ label: 'Рецепти по час', data: stats.hourValues, backgroundColor: colors.primary }]
            },
            options: barOptions
        });
    }

    /* -----------------------------------------------------------------------
       UserStats page
       ----------------------------------------------------------------------- */

    // Bar chart: number of recipes per user.
    const recipesCanvas = document.getElementById('recipesChart');
    if (recipesCanvas) {
        new Chart(recipesCanvas, {
            type: 'bar',
            data: {
                labels: stats.userNames,
                datasets: [{ label: 'Брой рецепти', data: stats.recipeCounts, backgroundColor: colors.primary }]
            },
            options: barOptions
        });
    }

    // Bar chart: likes each user's recipes received.
    const likesCanvas = document.getElementById('likesChart');
    if (likesCanvas) {
        new Chart(likesCanvas, {
            type: 'bar',
            data: {
                labels: stats.userNames,
                datasets: [{ label: 'Харесвания на рецепти', data: stats.likesReceived, backgroundColor: colors.accent }]
            },
            options: barOptions
        });
    }
});

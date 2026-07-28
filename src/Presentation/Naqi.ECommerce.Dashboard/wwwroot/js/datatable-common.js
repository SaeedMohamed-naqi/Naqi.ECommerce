// src/Presentation/Naqi.ECommerce.Dashboard/wwwroot/js/datatable-common.js
//
// Shared config for every server-side DataTable across the Dashboard
// (Products, and whatever list pages follow - Categories, Orders,
// Customers...). Each page only supplies what's actually DIFFERENT:
// the ajax url, columns, order, and optional search/count-badge/empty-message
// hooks. Everything else (dom layout, processing text, pagination classes,
// RTL language file, debounced search wiring) lives here once.
//
// Culture is read from window.NaqiDashboard (set once in _Layout.cshtml),
// not from a per-page Razor interpolation - see _Layout.cshtml's inline
// script block for where that global gets set.

(function () {
    "use strict";

    const ARABIC_LANGUAGE_URL = "https://cdn.datatables.net/plug-ins/1.13.8/i18n/ar.json";

    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function isArabic() {
        return window.NaqiDashboard && window.NaqiDashboard.currentCulture === "ar";
    }

    /**
     * Initializes a server-side DataTable with the dashboard's shared config.
     *
     * @param {string} tableSelector - e.g. '#products-table'
     * @param {object} config
     * @param {string} config.url - ajax endpoint (required)
     * @param {object} [config.data] - extra static POST data to send, if any
     * @param {Array}  config.columns - column definitions (required)
     * @param {Array}  [config.order] - e.g. [[6, 'desc']]
     * @param {number} [config.pageLength=20]
     * @param {string} [config.emptyMessage] - HTML shown when there's no data
     * @param {string} [config.countBadgeSelector] - element to update with recordsFiltered on every draw
     * @param {string} [config.searchInputSelector] - input wired to table.search() with debounce
     * @param {object} [config.dataTableOptions] - any extra raw DataTables options to merge in/override
     * @returns {DataTable} the underlying DataTables API instance
     */
    window.NaqiDataTable = {
        init: function (tableSelector, config) {
            const options = Object.assign(
                {
                    serverSide: true,
                    processing: true,
                    dom: 'rt<"d-flex justify-content-between align-items-center flex-wrap mt-3"ip>',
                    pageLength: config.pageLength || 20,
                    language: {
                        processing: "",
                        emptyTable: config.emptyMessage || "",
                        url: isArabic() ? ARABIC_LANGUAGE_URL : ""
                    }
                },
                config.dataTableOptions || {}
            );

            options.ajax = {
                url: config.url,
                type: "POST",
                headers: { RequestVerificationToken: getAntiForgeryToken() },
                data: config.data
            };

            options.columns = config.columns;
            if (config.order) options.order = config.order;

            if (config.countBadgeSelector) {
                options.drawCallback = function (settings) {
                    const el = document.querySelector(config.countBadgeSelector);
                    if (el) el.textContent = settings.json ? settings.json.recordsFiltered : 0;
                };
            }

            const table = $(tableSelector).DataTable(options);

            if (config.searchInputSelector) {
                let searchTimeout;
                $(config.searchInputSelector).on("keyup", function () {
                    clearTimeout(searchTimeout);
                    const value = this.value;
                    searchTimeout = setTimeout(() => table.search(value).draw(), 300);
                });
            }

            return table;
        }
    };
})();
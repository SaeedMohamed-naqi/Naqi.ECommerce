// src/Presentation/Naqi.ECommerce.Dashboard/wwwroot/js/datetime-common.js
//
// Every timestamp coming from the server (LastSyncedAtUtc, CreatedAtUtc,
// etc.) is stored and transmitted as UTC. This converts it to whatever
// timezone the BROWSER is actually in - JS's Date object does this
// automatically the moment you construct it from an ISO/UTC string and
// call toLocaleString() without an explicit timeZone override, so no
// manual offset math is needed here.
//
// Locale-aware too: reads window.NaqiDashboard.currentCulture (set once
// in _Layout.cshtml) so Arabic pages get Arabic-formatted dates/numbers
// and English pages get English formatting, without each page having to
// know or care which culture is active.

(function () {
    "use strict";

    function getLocale() {
        return window.NaqiDashboard && window.NaqiDashboard.currentCulture === "ar" ? "ar-SA" : "en-US";
    }

    function parseDate(utcValue) {
        if (!utcValue) return null;
        const date = new Date(utcValue);
        return isNaN(date.getTime()) ? null : date;
    }

    window.NaqiDateTime = {
        /**
         * Formats a UTC timestamp in the browser's local timezone.
         * @param {string} utcValue - ISO/UTC date string from the server (or null)
         * @param {object} [options] - overrides merged into Intl.DateTimeFormat options
         * @returns {string} e.g. "Jul 28, 2026, 03:45 PM" (en) or Arabic equivalent
         */
        toLocal: function (utcValue, options) {
            const date = parseDate(utcValue);
            if (!date) return "-";

            const defaultOptions = {
                year: "numeric",
                month: "short",
                day: "numeric",
                hour: "2-digit",
                minute: "2-digit"
            };

            return date.toLocaleString(getLocale(), Object.assign({}, defaultOptions, options));
        },

        /**
         * Date only, no time - e.g. for "Created" columns where time-of-day
         * isn't meaningful.
         */
        toLocalDate: function (utcValue) {
            const date = parseDate(utcValue);
            if (!date) return "-";

            return date.toLocaleDateString(getLocale(), { year: "numeric", month: "short", day: "numeric" });
        },

        /**
         * Relative phrasing ("2 hours ago" / "منذ ساعتين") - nicer for activity
         * feeds/recent-sync displays than an absolute timestamp.
         */
        toRelative: function (utcValue) {
            const date = parseDate(utcValue);
            if (!date) return "-";

            const rtf = new Intl.RelativeTimeFormat(getLocale(), { numeric: "auto" });
            const diffSeconds = Math.round((date.getTime() - Date.now()) / 1000);

            const divisions = [
                { amount: 60, unit: "seconds" },
                { amount: 60, unit: "minutes" },
                { amount: 24, unit: "hours" },
                { amount: 7, unit: "days" },
                { amount: 4.34524, unit: "weeks" },
                { amount: 12, unit: "months" },
                { amount: Number.POSITIVE_INFINITY, unit: "years" }
            ];

            let duration = diffSeconds;
            for (const division of divisions) {
                if (Math.abs(duration) < division.amount) {
                    return rtf.format(Math.round(duration), division.unit);
                }
                duration /= division.amount;
            }
            return "-";
        }
    };
})();
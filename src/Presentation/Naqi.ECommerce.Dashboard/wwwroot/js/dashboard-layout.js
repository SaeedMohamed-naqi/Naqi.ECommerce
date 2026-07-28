// src/Presentation/Naqi.ECommerce.Dashboard/wwwroot/js/dashboard-layout.js
//
// Ported behavior from the React Navbar/Sidebar:
//   - dark mode toggled via a body.dark-mode class, persisted to localStorage
//   - sidebar collapse state persisted to localStorage
// Language switching is NOT handled here - that's server-side via
// CultureController + a full page reload (see _Layout.cshtml), since this
// is a server-rendered Razor app, not an SPA with react-i18next.

(function () {
    "use strict";

    // ---- Dark mode ----
    const THEME_KEY = "theme";

    function applyTheme(theme) {
        document.body.classList.toggle("dark-mode", theme === "dark");
    }

    function initTheme() {
        const saved = localStorage.getItem(THEME_KEY) || "light";
        applyTheme(saved);
    }

    function toggleTheme() {
        const isDark = document.body.classList.contains("dark-mode");
        const next = isDark ? "light" : "dark";
        applyTheme(next);
        localStorage.setItem(THEME_KEY, next);
    }

    // ---- Sidebar collapse ----
    const SIDEBAR_KEY = "sidebarCollapsed";

    function applySidebarState(collapsed) {
        const sidebar = document.querySelector(".sidebar");
        if (sidebar) sidebar.classList.toggle("collapsed", collapsed);
    }

    function initSidebar() {
        const saved = localStorage.getItem(SIDEBAR_KEY) === "true";
        applySidebarState(saved);
    }

    function toggleSidebar() {
        const sidebar = document.querySelector(".sidebar");
        const collapsed = sidebar ? !sidebar.classList.contains("collapsed") : false;
        applySidebarState(collapsed);
        localStorage.setItem(SIDEBAR_KEY, String(collapsed));
    }

    document.addEventListener("DOMContentLoaded", function () {
        initTheme();
        initSidebar();

        const themeToggleBtn = document.getElementById("theme-toggle");
        if (themeToggleBtn) themeToggleBtn.addEventListener("click", toggleTheme);

        const sidebarToggleBtn = document.getElementById("sidebar-toggle");
        if (sidebarToggleBtn) sidebarToggleBtn.addEventListener("click", toggleSidebar);

        // Collapsed-state logo re-expands the sidebar on click - only
        // visible via CSS when .sidebar.collapsed is active.
        const collapsedLogo = document.getElementById("sidebar-logo-collapsed");
        if (collapsedLogo) collapsedLogo.addEventListener("click", toggleSidebar);
    });
})();
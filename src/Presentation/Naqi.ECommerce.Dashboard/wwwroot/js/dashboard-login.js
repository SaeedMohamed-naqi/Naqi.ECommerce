// src/Presentation/Naqi.ECommerce.Dashboard/wwwroot/js/dashboard-login.js
//
// Ported from the React LoginPage's useEffect-driven slider (setInterval +
// fade timing) and the segmented system toggle. This is vanilla JS since
// the Dashboard is server-rendered Razor, not a React SPA.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initSlider();
        initSystemToggle();
    });

    function initSlider() {
        const imageEl = document.getElementById("login-slide-image");
        if (!imageEl) return;

        // Populated by Login.cshtml into data-* attributes - see the view.
        const isRtl = imageEl.dataset.rtl === "true";
        const images = isRtl
            ? JSON.parse(imageEl.dataset.imagesRtl || "[]")
            : JSON.parse(imageEl.dataset.imagesLtr || "[]");

        const overlayHeading = document.getElementById("login-overlay-heading");
        const overlaySubtext = document.getElementById("login-overlay-subtext");
        const overlays = JSON.parse(imageEl.dataset.overlays || "[]");

        if (images.length === 0) return;

        let currentIndex = 0;

        setInterval(function () {
            imageEl.classList.add("fade-out");
            imageEl.classList.remove("fade-in");

            setTimeout(function () {
                currentIndex = (currentIndex + 1) % images.length;
                imageEl.src = images[currentIndex];

                if (overlays[currentIndex]) {
                    overlayHeading.textContent = overlays[currentIndex].heading;
                    overlaySubtext.textContent = overlays[currentIndex].subtext;
                }

                imageEl.classList.remove("fade-out");
                imageEl.classList.add("fade-in");
            }, 300);
        }, 5000);
    }

    function initSystemToggle() {
        const buttons = document.querySelectorAll(".system-option");
        const hiddenInput = document.getElementById("System");
        if (buttons.length === 0 || !hiddenInput) return;

        buttons.forEach(function (button) {
            button.addEventListener("click", function () {
                buttons.forEach(function (b) { b.classList.remove("system-active"); });
                button.classList.add("system-active");
                hiddenInput.value = button.dataset.system;
            });
        });
    }
})();
// src/Presentation/Naqi.ECommerce.Dashboard/wwwroot/js/categories-sync.js

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        const btn = document.getElementById("sync-categories-btn");
        const icon = document.getElementById("sync-categories-icon");
        const label = document.getElementById("sync-categories-label");
        if (!btn) return;

        const originalLabel = label.textContent;
        const successTitle = btn.dataset.successTitle;
        const successTextTemplate = btn.dataset.successText; // "{fetched} fetched, {created} created, {updated} updated"
        const errorTitle = btn.dataset.errorTitle;

        btn.addEventListener("click", async function () {
            btn.disabled = true;
            icon.classList.add("fa-spin");
            label.textContent = "...";

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

                const response = await fetch("/Categories/Sync", {
                    method: "POST",
                    headers: {
                        "RequestVerificationToken": token || ""
                    }
                });

                const data = await response.json();

                if (data.success) {
                    const summary = data.data; // ApiResponse<T>.Data
                    const text = successTextTemplate
                        .replace("{fetched}", summary.totalProcessed)
                        .replace("{created}", summary.created)
                        .replace("{updated}", summary.updated);

                    await Swal.fire({
                        icon: "success",
                        title: successTitle,
                        text: text,
                        confirmButtonColor: "#3553ff"
                    });
                    window.location.reload();
                } else {
                    await Swal.fire({
                        icon: "error",
                        title: errorTitle,
                        text: data.message,
                        confirmButtonColor: "#3553ff"
                    });
                }
            } catch (err) {
                await Swal.fire({
                    icon: "error",
                    title: errorTitle,
                    text: err.message,
                    confirmButtonColor: "#3553ff"
                });
            } finally {
                btn.disabled = false;
                icon.classList.remove("fa-spin");
                label.textContent = originalLabel;
            }
        });
    });
})();
document.addEventListener("DOMContentLoaded", function () {

    const searchInput = document.getElementById("productSearchInput");
    const searchForm = document.getElementById("productSearchForm");

    if (!searchInput || !searchForm) return;

    const products = document.querySelectorAll(".product-item");

    function filterProducts() {
        const keyword = searchInput.value.toLowerCase().trim();

        products.forEach(p => {
            const name = p.dataset.name.toLowerCase();
            const category = p.dataset.category.toLowerCase();

            // Show all if keyword is empty, otherwise filter
            if (!keyword || name.includes(keyword) || category.includes(keyword)) {
                p.style.display = "";
            } else {
                p.style.display = "none";
            }
        });
    }

    // Only search on form submit
    searchForm.addEventListener("submit", function (e) {
        e.preventDefault(); // prevent page reload
        filterProducts();
    });

    // Automatically show all products if input is empty
    searchInput.addEventListener("input", function () {
        if (searchInput.value.trim() === "") {
            products.forEach(p => p.style.display = "");
        }
    });
      // === tracking order ===
    document.addEventListener("DOMContentLoaded", function () {
        // Select the track order form
        const trackOrderForm = document.querySelector(".track-order-form");
        if (!trackOrderForm) return;

        trackOrderForm.addEventListener("submit", function (e) {
            e.preventDefault(); // prevent normal form submission

            const trackingIdInput = trackOrderForm.querySelector('input[name="trackingId"]');
            if (!trackingIdInput || !trackingIdInput.value.trim()) {
                alert("Please enter a tracking ID!");
                return;
            }

            const trackingId = trackingIdInput.value.trim();

            // Make AJAX request to the server
            fetch(`/Checkout/TrackOrder?trackingId=${encodeURIComponent(trackingId)}`)
                .then(response => {
                    if (!response.ok) throw new Error("Network response was not ok");
                    return response.text(); // get HTML
                })
                .then(html => {
                    // Render response HTML in a container
                    let resultContainer = document.getElementById("trackOrderResult");
                    if (!resultContainer) {
                        resultContainer = document.createElement("div");
                        resultContainer.id = "trackOrderResult";
                        trackOrderForm.parentNode.appendChild(resultContainer);
                    }
                    resultContainer.innerHTML = html;
                })
                .catch(error => {
                    console.error("Error fetching order status:", error);
                    alert("Something went wrong. Please try again later.");
                });
        });
    });

});

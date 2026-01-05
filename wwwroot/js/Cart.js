document.addEventListener("DOMContentLoaded", function () {

    // ------------------------
    // Proceed to Checkout Button
    // ------------------------
    const btnCheckout = document.getElementById("btnCheckout");
    if (btnCheckout) {
        btnCheckout.addEventListener("click", function () {
            const checkoutUrl = this.getAttribute("data-checkout-url");
            if (checkoutUrl) {
                window.location.href = checkoutUrl;
            }
        });
    }

    // ------------------------
    // Validate Quantity Inputs
    // ------------------------
    const quantityInputs = document.querySelectorAll('input[type="number"][name="quantity"]');
    quantityInputs.forEach(input => {
        input.addEventListener("change", function () {
            const stock = parseInt(this.getAttribute("data-stock"));
            const productName = this.getAttribute("data-name");
            let quantity = parseInt(this.value);

            if (quantity > stock) {
                alert(`Sorry! Only ${stock} units of "${productName}" are available in stock.`);
                this.value = stock;
                this.form.submit();
            } else if (quantity < 1) {
                this.value = 1;
                this.form.submit();
            }
        });
    });

    // ------------------------
    // Popup Modal for "Coming Soon"
    // ------------------------
    const comingSoonLinks = document.querySelectorAll('.coming-soon');
    const modal = document.getElementById("popupModal");
    if (modal) {
        const closeBtn = modal.querySelector(".close");

        // Close button
        if (closeBtn) {
            closeBtn.addEventListener("click", function () {
                modal.style.display = "none";
            });
        }

        // Click outside modal
        window.addEventListener("click", function (event) {
            if (event.target == modal) {
                modal.style.display = "none";
            }
        });

        // Open modal on "Coming Soon" links
        comingSoonLinks.forEach(link => {
            link.addEventListener("click", function (e) {
                e.preventDefault();
                modal.style.display = "block";
            });
        });
    }
});

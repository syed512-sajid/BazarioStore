document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("checkoutForm");
    if (!form) return;

    const loader = document.getElementById("orderLoader");
    const toastEl = document.getElementById("orderToast");
    const toastMessageEl = document.getElementById("toastMessage");
    const toast = toastEl ? new bootstrap.Toast(toastEl, { delay: 4000 }) : null;

    // Get Anti-forgery token
    function getToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput) {
            console.error("Anti-forgery token not found!");
            alert("Token missing! Refresh the page.");
            return null;
        }
        return tokenInput.value;
    }

    // Show toast helper
    function showToast(type, message) {
        if (!toastEl || !toastMessageEl) return;
        toastMessageEl.textContent = message;
        toastEl.classList.remove('bg-success', 'bg-danger');
        toastEl.classList.add(type === 'success' ? 'bg-success' : 'bg-danger');
        toast.show();
    }

    /* =========================
       POPUP MODAL FOR PAYMENT (COMING SOON)
    ========================= */
    const popupModalEl = document.getElementById("popupModal");
    let popupModal = null;
    if (popupModalEl) {
        popupModal = new bootstrap.Modal(popupModalEl, { backdrop: 'static', keyboard: true });
    }

    const paymentSelect = document.getElementById("paymentMethod");
    const unavailablePayments = ["JazzCash", "EasyPaisa", "Bank"];
    if (paymentSelect) {
        paymentSelect.addEventListener("change", function () {
            if (unavailablePayments.includes(this.value)) {
                if (popupModal && popupModalEl) {
                    const modalText = popupModalEl.querySelector("p");
                    if (modalText) modalText.textContent = `${this.value} payment method is coming soon!`;
                    popupModal.show();
                } else {
                    alert(this.value + " payment method is coming soon!");
                }
                this.value = "COD";
            }
        });
    }

    /* =========================
       FORM VALIDATION (Email & Phone)
    ========================= */
    const requiredInputs = form.querySelectorAll('input[required], textarea[required], select[required]');
    requiredInputs.forEach(input => {
        input.addEventListener("blur", () => {
            if (!input.value.trim()) {
                input.classList.add("is-invalid");
                input.classList.remove("is-valid");
            } else {
                input.classList.remove("is-invalid");
                input.classList.add("is-valid");
            }
        });
        input.addEventListener("input", () => input.classList.remove("is-invalid"));
    });

    const emailInput = form.querySelector('input[type="email"]');
    if (emailInput) {
        emailInput.addEventListener("blur", () => {
            const pattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!pattern.test(emailInput.value)) {
                emailInput.classList.add("is-invalid");
                emailInput.classList.remove("is-valid");
            } else {
                emailInput.classList.remove("is-invalid");
                emailInput.classList.add("is-valid");
            }
        });
    }

    const phoneInput = form.querySelector('input[type="tel"]');
    if (phoneInput) {
        phoneInput.addEventListener("input", () => {
            phoneInput.value = phoneInput.value.replace(/[^\d\s\-\+\(\)]/g, '');
        });
        phoneInput.addEventListener("blur", () => {
            const digits = phoneInput.value.replace(/\D/g, '');
            if (digits.length < 10) {
                phoneInput.classList.add("is-invalid");
                phoneInput.classList.remove("is-valid");
            } else {
                phoneInput.classList.remove("is-invalid");
                phoneInput.classList.add("is-valid");
            }
        });
    }

    /* =========================
       FORM SUBMIT
    ========================= */
    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const token = getToken();
        if (!token) return;

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;

        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processing...';
        loader.classList.remove("d-none");

        try {
            const response = await fetch(form.action || "/Checkout/PlaceOrder", {
                method: "POST",
                body: new FormData(form),
                headers: {
                    "RequestVerificationToken": token
                }
            });

            const data = await response.json();
            loader.classList.add("d-none");
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            if (response.ok && data.success) {
                showToast('success', data.message || "Order placed successfully!");
                setTimeout(() => window.location.href = `/Checkout/OrderConfirmation/${data.orderId}`, 1500);
            } else {
                showToast('error', data.message || "Failed to place order. Try again.");
            }
        } catch (err) {
            console.error(err);
            loader.classList.add("d-none");
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
            showToast('error', "Server error occurred. Please try again.");
        }
    });
});

document.addEventListener("DOMContentLoaded", function () {

    const form = document.getElementById("checkoutForm");
    if (!form) return;

    /* =========================
       POPUP MODAL (SAFE)
    ========================= */
    const popupModalEl = document.getElementById("popupModal");
    let popupModal = null;

    if (popupModalEl) {
        popupModal = new bootstrap.Modal(popupModalEl, {
            backdrop: 'static',
            keyboard: true
        });
    }

    /* =========================
       PAYMENT METHOD (COMING SOON)
    ========================= */
    const paymentSelect = document.getElementById("paymentMethod");
    const unavailablePayments = ["JazzCash", "EasyPaisa", "Bank"];

    if (paymentSelect) {
        paymentSelect.addEventListener("change", function () {
            if (unavailablePayments.includes(this.value)) {

                if (popupModal && popupModalEl) {
                    const modalText = popupModalEl.querySelector("p");
                    if (modalText) {
                        modalText.textContent = `${this.value} payment method is coming soon!`;
                    }
                    popupModal.show();
                } else {
                    alert(this.value + " payment method is coming soon!");
                }

                this.value = "COD";
            }
        });
    }

    /* =========================
       FORM SUBMIT
    ========================= */
    form.addEventListener("submit", function (e) {
        e.preventDefault();

        // Validate form
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;

        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processing Order...';

        const formData = new FormData(form);

        fetch("/Checkout/PlaceOrder", {
            method: "POST",
            body: formData
        })
            .then(res => {
                if (!res.ok) {
                    throw new Error("Server error: " + res.status);
                }
                return res.json();
            })
            .then(data => {
                if (data.success) {
                    // Update toast message
                    const toastMessageEl = document.getElementById("toastMessage");
                    if (toastMessageEl) {
                        toastMessageEl.textContent = data.message || "Order placed successfully!";
                    }

                    // Show toast
                    const toastEl = document.getElementById("orderToast");
                    if (toastEl) {
                        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
                        toast.show();
                    }

                    // Redirect after delay
                    setTimeout(() => {
                        window.location.href = `/Checkout/OrderConfirmation/${data.orderId}`;
                    }, 2000);

                } else {
                    throw new Error(data.message || "Order failed");
                }
            })
            .catch(err => {
                console.error("Checkout error:", err);

                // Show error toast
                const toastEl = document.getElementById("orderToast");
                if (toastEl) {
                    toastEl.classList.remove('bg-success');
                    toastEl.classList.add('bg-danger');

                    const toastMessageEl = document.getElementById("toastMessage");
                    if (toastMessageEl) {
                        toastMessageEl.textContent = err.message || "An error occurred. Please try again.";
                    }

                    const toast = new bootstrap.Toast(toastEl);
                    toast.show();

                    // Reset toast color after hiding
                    toastEl.addEventListener('hidden.bs.toast', function () {
                        toastEl.classList.remove('bg-danger');
                        toastEl.classList.add('bg-success');
                    }, { once: true });
                } else {
                    alert(err.message || "An error occurred. Please try again.");
                }

                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            });
    });

    /* =========================
       FORM VALIDATION
    ========================= */
    const requiredInputs = form.querySelectorAll('input[required], textarea[required], select[required]');

    requiredInputs.forEach(input => {
        input.addEventListener("blur", function () {
            if (!this.value.trim()) {
                this.classList.add("is-invalid");
                this.classList.remove("is-valid");
            } else {
                this.classList.remove("is-invalid");
                this.classList.add("is-valid");
            }
        });

        input.addEventListener("input", function () {
            if (this.value.trim()) {
                this.classList.remove("is-invalid");
            }
        });
    });

    /* =========================
       EMAIL VALIDATION
    ========================= */
    const emailInput = form.querySelector('input[type="email"]');
    if (emailInput) {
        emailInput.addEventListener("blur", function () {
            const pattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!pattern.test(this.value)) {
                this.classList.add("is-invalid");
                this.classList.remove("is-valid");
            } else {
                this.classList.remove("is-invalid");
                this.classList.add("is-valid");
            }
        });
    }
    document.getElementById("checkoutForm").addEventListener("submit", function (e) {
        e.preventDefault();

        const loader = document.getElementById("orderLoader");
        loader.classList.remove("d-none");

        const formData = new FormData(this);

        fetch("/Checkout/PlaceOrder", {
            method: "POST",
            body: formData,
            headers: {
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
            .then(res => res.json())
            .then(data => {
                loader.classList.add("d-none");

                if (data.success) {
                    window.location.href = `/Checkout/OrderConfirmation/${data.orderId}`;
                } else {
                    alert(data.message);
                }
            })
            .catch(() => {
                loader.classList.add("d-none");
                alert("Order failed. Please try again.");
            });
    });

    /* =========================
       PHONE VALIDATION
    ========================= */
    const phoneInput = form.querySelector('input[type="tel"]');
    if (phoneInput) {
        phoneInput.addEventListener("input", function () {
            // Allow only numbers, spaces, dashes, plus, and parentheses
            this.value = this.value.replace(/[^\d\s\-\+\(\)]/g, '');
        });

        phoneInput.addEventListener("blur", function () {
            // Check if phone has at least 10 digits
            const digitsOnly = this.value.replace(/\D/g, '');
            if (digitsOnly.length < 10) {
                this.classList.add("is-invalid");
                this.classList.remove("is-valid");
            } else {
                this.classList.remove("is-invalid");
                this.classList.add("is-valid");
            }
        });
    }
});
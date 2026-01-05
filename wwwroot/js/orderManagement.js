document.addEventListener("DOMContentLoaded", () => {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    if (!tokenInput) {
        console.error("Anti-forgery token not found");
        return;
    }

    const loadingModal = new bootstrap.Modal(document.getElementById('loadingModal'));

    document.querySelectorAll(".order-action").forEach(btn => {
        btn.addEventListener("click", async function () {
            // Prevent double clicks
            if (this.disabled) return;

            const orderId = this.dataset.orderid;
            const action = this.dataset.action;

            // Confirmation dialog with action-specific messages
            let confirmMessage = "";
            let actionTitle = "";

            switch (action) {
                case "delivered":
                    actionTitle = "Mark as Delivered";
                    confirmMessage = "Are you sure you want to mark this order as delivered?\n\n✅ The customer will receive a delivery confirmation email.\n⚠️ This action cannot be undone.";
                    break;
                case "cancel":
                    actionTitle = "Cancel Order";
                    confirmMessage = "Are you sure you want to cancel this order?\n\n❌ The customer will be notified about the cancellation.\n⚠️ This action cannot be undone.";
                    break;
                case "notaccept":
                    actionTitle = "Not Accept Order";
                    confirmMessage = "Are you sure you want to reject this order?\n\n⛔ The customer will be notified that their order was not accepted.\n⚠️ This action cannot be undone.";
                    break;
                default:
                    return;
            }

            // Show confirmation dialog
            if (!confirm(confirmMessage)) {
                return;
            }

            // Disable all action buttons
            document.querySelectorAll(".order-action").forEach(b => b.disabled = true);

            // Show loading modal
            loadingModal.show();

            // Prepare URL and payload
            let url = "";
            const payload = { orderId: parseInt(orderId) };

            switch (action) {
                case "delivered":
                    url = "/Admin/MarkAsDelivered";
                    break;
                case "cancel":
                    url = "/Admin/CancelOrder";
                    break;
                case "notaccept":
                    url = "/Admin/NotAcceptOrder";
                    break;
            }

            try {
                const response = await fetch(url, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": tokenInput.value
                    },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                // Hide loading modal
                loadingModal.hide();

                if (response.ok && result.success) {
                    // Show success message
                    showToast("success", result.message || "Order updated successfully!");

                    // Reload page after short delay to show updated status
                    setTimeout(() => {
                        location.reload();
                    }, 1500);
                } else {
                    // Show error message
                    showToast("error", result.message || "Failed to update order. Please try again.");

                    // Re-enable buttons
                    document.querySelectorAll(".order-action").forEach(b => b.disabled = false);
                }
            } catch (error) {
                console.error("Error:", error);

                // Hide loading modal
                loadingModal.hide();

                // Show error message
                showToast("error", "A server error occurred. Please try again.");

                // Re-enable buttons
                document.querySelectorAll(".order-action").forEach(b => b.disabled = false);
            }
        });
    });

    // Toast notification function
    function showToast(type, message) {
        // Create toast container if it doesn't exist
        let toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        // Determine colors based on type
        const bgColor = type === 'success' ? 'bg-success' : 'bg-danger';
        const icon = type === 'success' ? 'check-circle-fill' : 'exclamation-triangle-fill';

        // Create toast HTML
        const toastHtml = `
            <div class="toast align-items-center text-white ${bgColor} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="bi bi-${icon} me-2"></i>
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        // Add toast to container
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);

        // Get the newly added toast
        const toastElement = toastContainer.lastElementChild;
        const toast = new bootstrap.Toast(toastElement, { delay: 4000 });

        // Show toast
        toast.show();

        // Remove toast from DOM after it's hidden
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    }
});

// Order filter tabs (for Orders list page)
document.addEventListener('DOMContentLoaded', function () {
    const filterLinks = document.querySelectorAll('.filter-tabs .nav-link');
    const orderItems = document.querySelectorAll('.order-item');

    if (filterLinks.length > 0) {
        filterLinks.forEach(link => {
            link.addEventListener('click', function (e) {
                e.preventDefault();

                // Update active tab
                filterLinks.forEach(l => l.classList.remove('active'));
                this.classList.add('active');

                const filter = this.getAttribute('data-filter');

                // Filter orders
                orderItems.forEach(item => {
                    if (filter === 'all' || item.getAttribute('data-status') === filter) {
                        item.style.display = '';
                    } else {
                        item.style.display = 'none';
                    }
                });
            });
        });
    }
});
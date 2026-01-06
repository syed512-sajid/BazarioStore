document.addEventListener("DOMContentLoaded", () => {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]'); // may be null

    const loadingModalEl = document.getElementById('loadingModal');
    const loadingModal = loadingModalEl ? new bootstrap.Modal(loadingModalEl) : null;

    document.querySelectorAll(".order-action").forEach(btn => {
        btn.addEventListener("click", async function () {
            if (this.disabled) return;

            const orderId = this.dataset.orderid;
            const action = this.dataset.action;

            // Actions that require anti-forgery token
            if (!tokenInput) {
                console.warn("Anti-forgery token missing. Cannot perform action:", action);
                showToast("error", "Anti-forgery token missing. Refresh page to perform this action.");
                return;
            }

            // Confirmation dialog
            let confirmMessage = "";
            switch (action) {
                case "delivered":
                    confirmMessage = "Are you sure you want to mark this order as delivered?\nThis cannot be undone.";
                    break;
                case "cancel":
                    confirmMessage = "Are you sure you want to cancel this order?\nThis cannot be undone.";
                    break;
                case "notaccept":
                    confirmMessage = "Are you sure you want to reject this order?\nThis cannot be undone.";
                    break;
                default:
                    return;
            }

            if (!confirm(confirmMessage)) return;

            // Disable buttons and show loading
            document.querySelectorAll(".order-action").forEach(b => b.disabled = true);
            if (loadingModal) loadingModal.show();

            const urlMap = {
                delivered: "/Admin/MarkAsDelivered",
                cancel: "/Admin/CancelOrder",
                notaccept: "/Admin/NotAcceptOrder"
            };
            const url = urlMap[action];
            const payload = { orderId: parseInt(orderId) };

            try {
                const res = await fetch(url, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": tokenInput.value
                    },
                    body: JSON.stringify(payload)
                });

                const result = await res.json();

                if (loadingModal) loadingModal.hide();

                if (res.ok && result.success) {
                    showToast("success", result.message || "Order updated successfully!");
                    setTimeout(() => location.reload(), 1500);
                } else {
                    showToast("error", result.message || "Failed to update order.");
                    document.querySelectorAll(".order-action").forEach(b => b.disabled = false);
                }
            } catch (err) {
                console.error(err);
                if (loadingModal) loadingModal.hide();
                showToast("error", "Server error occurred.");
                document.querySelectorAll(".order-action").forEach(b => b.disabled = false);
            }
        });
    });

    function showToast(type, message) {
        let toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const bgColor = type === 'success' ? 'bg-success' : 'bg-danger';
        const icon = type === 'success' ? 'check-circle-fill' : 'exclamation-triangle-fill';

        const toastHtml = `
            <div class="toast align-items-center text-white ${bgColor} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="bi bi-${icon} me-2"></i>${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = toastContainer.lastElementChild;
        const toast = new bootstrap.Toast(toastElement, { delay: 4000 });
        toast.show();
        toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
    }

    // FILTER TABS (Orders List)
    const filterLinks = document.querySelectorAll('.filter-tabs .nav-link');
    const orderItems = document.querySelectorAll('.order-item');

    if (filterLinks.length && orderItems.length) {
        filterLinks.forEach(link => {
            link.addEventListener('click', e => {
                e.preventDefault();
                filterLinks.forEach(l => l.classList.remove('active'));
                link.classList.add('active');

                const filter = link.dataset.filter;
                orderItems.forEach(item => {
                    item.style.display = filter === 'all' || item.dataset.status === filter ? '' : 'none';
                });
            });
        });
    }
});

//document.addEventListener("DOMContentLoaded", () => {

//    /* ===============================
//       FILTER TABS (ORDER STATUS)
//    =============================== */

//    const filterLinks = document.querySelectorAll(".filter-tabs .nav-link");
//    const orderItems = document.querySelectorAll(".order-item");

//    filterLinks.forEach(link => {
//        link.addEventListener("click", e => {
//            e.preventDefault();

//            filterLinks.forEach(l => l.classList.remove("active"));
//            link.classList.add("active");

//            const filter = link.dataset.filter;

//            orderItems.forEach(item => {
//                const status = item.dataset.status;
//                item.style.display =
//                    filter === "all" || status === filter ? "block" : "none";
//            });
//        });
//    });


//    /* ===============================
//       ORDER ACTIONS (MODAL + TOAST)
//    =============================== */

//    const tokenInput = document.querySelector(
//        'input[name="__RequestVerificationToken"]'
//    );

//    if (!tokenInput) {
//        console.error("Anti-forgery token not found");
//        return;
//    }

//    let currentBtn = null;
//    let currentAction = "";
//    let currentOrderId = "";

//    const reasonModalEl = document.getElementById("reasonModal");
//    let reasonModal = reasonModalEl ? new bootstrap.Modal(reasonModalEl) : null;

//    const toastEl = document.getElementById("actionToast");
//    let toast = toastEl ? new bootstrap.Toast(toastEl) : null;

//    if (reasonModalEl) {
//        reasonModal = new bootstrap.Modal(reasonModalEl, {
//            backdrop: true,
//            keyboard: true,
//            focus: true
//        });

//        // Reset modal state when closed
//        reasonModalEl.addEventListener('hidden.bs.modal', () => {
//            const reasonText = document.getElementById("reasonText");
//            const reasonError = document.getElementById("reasonError");

//            if (reasonText) reasonText.value = "";
//            if (reasonError) reasonError.classList.add("d-none");

//            if (currentBtn) {
//                currentBtn.disabled = false;
//            }
//            currentBtn = null;
//            currentAction = "";
//            currentOrderId = "";
//        });

//        // Ensure modal closes properly when clicking close/cancel
//        reasonModalEl.querySelectorAll('[data-bs-dismiss="modal"]').forEach(btn => {
//            btn.addEventListener('click', (e) => {
//                e.preventDefault();
//                reasonModal.hide();
//            });
//        });
//    }

//    const toastEl = document.getElementById("actionToast");
//    let toast = null;

//    if (toastEl) {
//        toast = new bootstrap.Toast(toastEl, {
//            autohide: true,
//            delay: 3000
//        });

//        // Ensure toast hides after showing
//        toastEl.addEventListener('shown.bs.toast', () => {
//            setTimeout(() => {
//                toast.hide();
//            }, 3000);
//        });
//    }

//    // Handle order action buttons
//    document.querySelectorAll(".order-action").forEach(btn => {
//        btn.addEventListener("click", (e) => {
//            e.preventDefault();
//            e.stopPropagation();

//            currentBtn = btn;
//            currentAction = btn.dataset.action;
//            currentOrderId = btn.dataset.orderid;

//            // Processing and Delivered → no reason required
//            if (currentAction === "processing" || currentAction === "delivered") {
//                processAction("");
//                return;
//            }

//            // Cancel and NotAccept → show reason modal (optional now with auto-generated reasons)
//            // We'll skip the modal and use auto-generated reasons
//            processAction("");
//        });
//    });

//    // Handle confirm button in reason modal (keeping for future use if needed)
//    const confirmBtn = document.getElementById("confirmReasonBtn");
//    if (confirmBtn) {
//        confirmBtn.addEventListener("click", () => {
//            const reasonInput = document.getElementById("reasonText");
//            const reasonError = document.getElementById("reasonError");
//            const reason = reasonInput ? reasonInput.value.trim() : "";

//            if (!reason) {
//                if (reasonError) reasonError.classList.remove("d-none");
//                return;
//            }

//            if (reasonModal) {
//                reasonModal.hide();
//            }

//            // Small delay to ensure modal closes before processing
//            setTimeout(() => {
//                processAction(reason);
//            }, 300);
//        });
//    }

//    async function processAction(reason) {

//        if (currentBtn) {
//            currentBtn.disabled = true;
//        }

//        let url = "";
//        let payload = { orderId: currentOrderId };

//        switch (currentAction) {
//            case "processing":
//                url = "/Admin/MoveToProcessing";
//                break;

//            case "delivered":
//                url = "/Admin/MarkAsDelivered";
//                break;

//            case "cancel":
//                url = "/Admin/CancelOrder";
//                break;

//            case "notaccept":
//                url = "/Admin/NotAcceptOrder";
//                break;

//            default:
//                if (currentBtn) {
//                    currentBtn.disabled = false;
//                }
//                return;
//        }

//        try {
//            const res = await fetch(url, {
//                method: "POST",
//                headers: {
//                    "Content-Type": "application/json",
//                    "RequestVerificationToken": tokenInput.value
//                },
//                body: JSON.stringify(payload)
//            });

//            const result = await res.json();

//            if (res.ok && result.success) {
//                showToast(result.message || `Order ${currentAction.toUpperCase()} successfully`, 'success');
//                setTimeout(() => location.reload(), 1500);
//            } else {
//                showToast(result.message || "Action failed", 'danger');
//                if (currentBtn) {
//                    currentBtn.disabled = false;
//                }
//            }

//        } catch (err) {
//            console.error(err);
//            showToast("Server error occurred", 'danger');
//            if (currentBtn) {
//                currentBtn.disabled = false;
//            }
//        }
//    }

//    function showToast(message, type = 'success') {
//        if (!toast || !toastEl) return;

//        const toastMessage = document.getElementById("toastMessage");
//        if (toastMessage) {
//            toastMessage.innerText = message;
//        }

//        // Update toast color
//        toastEl.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-warning', 'text-bg-info');
//        toastEl.classList.add(`text-bg-${type}`);

//        toast.show();
//    }

//});

document.addEventListener("DOMContentLoaded", () => {
    if (typeof bootstrap === "undefined") {
        console.error("Bootstrap JS not loaded! Check your <script> order.");
        return;
    }

    /* ===============================
       FILTER TABS (ORDER STATUS)
    =============================== */
    const filterLinks = document.querySelectorAll(".filter-tabs .nav-link");
    const orderItems = document.querySelectorAll(".order-item");

    filterLinks.forEach(link => {
        link.addEventListener("click", e => {
            e.preventDefault();
            filterLinks.forEach(l => l.classList.remove("active"));
            link.classList.add("active");

            const filter = link.dataset.filter;
            orderItems.forEach(item => {
                const status = item.dataset.status;
                item.style.display = filter === "all" || status === filter ? "block" : "none";
            });
        });
    });

    /* ===============================
       ORDER ACTIONS (MODAL + TOAST)
    =============================== */
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) return console.error("Anti-forgery token not found");

    let currentBtn = null, currentAction = "", currentOrderId = "";

    // Initialize Reason Modal
    const reasonModalEl = document.getElementById("reasonModal");
    const reasonModal = reasonModalEl ? new bootstrap.Modal(reasonModalEl, { backdrop: true, keyboard: true, focus: true }) : null;

    if (reasonModalEl) {
        reasonModalEl.addEventListener('hidden.bs.modal', () => {
            const reasonText = document.getElementById("reasonText");
            const reasonError = document.getElementById("reasonError");
            if (reasonText) reasonText.value = "";
            if (reasonError) reasonError.classList.add("d-none");
            if (currentBtn) currentBtn.disabled = false;
            currentBtn = null;
            currentAction = "";
            currentOrderId = "";
        });

        reasonModalEl.querySelectorAll('[data-bs-dismiss="modal"]').forEach(btn => {
            btn.addEventListener('click', e => { e.preventDefault(); reasonModal.hide(); });
        });
    }

    // Initialize Toast
    const toastEl = document.getElementById("actionToast");
    const toast = toastEl ? new bootstrap.Toast(toastEl, { autohide: true, delay: 3000 }) : null;

    if (toastEl) {
        toastEl.addEventListener('shown.bs.toast', () => setTimeout(() => toast.hide(), 3000));
    }

    // Handle order action buttons
    document.querySelectorAll(".order-action").forEach(btn => {
        btn.addEventListener("click", e => {
            e.preventDefault(); e.stopPropagation();
            currentBtn = btn;
            currentAction = btn.dataset.action;
            currentOrderId = btn.dataset.orderid;

            // Skip modal for processing/delivered
            if (["processing", "delivered"].includes(currentAction)) {
                processAction("");
                return;
            }

            // Cancel / NotAccept → optional modal (currently auto reason)
            processAction("");
        });
    });

    // Handle confirm button in reason modal
    const confirmBtn = document.getElementById("confirmReasonBtn");
    if (confirmBtn) {
        confirmBtn.addEventListener("click", () => {
            const reasonInput = document.getElementById("reasonText");
            const reasonError = document.getElementById("reasonError");
            const reason = reasonInput ? reasonInput.value.trim() : "";

            if (!reason) { if (reasonError) reasonError.classList.remove("d-none"); return; }
            if (reasonModal) reasonModal.hide();
            setTimeout(() => processAction(reason), 300);
        });
    }

    async function processAction(reason) {
        if (currentBtn) currentBtn.disabled = true;
        let url = "", payload = { orderId: currentOrderId };

        switch (currentAction) {
            case "processing": url = "/Admin/MoveToProcessing"; break;
            case "delivered": url = "/Admin/MarkAsDelivered"; break;
            case "cancel": url = "/Admin/CancelOrder"; break;
            case "notaccept": url = "/Admin/NotAcceptOrder"; break;
            default: if (currentBtn) currentBtn.disabled = false; return;
        }

        try {
            const res = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": tokenInput.value },
                body: JSON.stringify(payload)
            });

            const result = await res.json();
            if (res.ok && result.success) {
                showToast(result.message || `Order ${currentAction.toUpperCase()} successfully`, 'success');
                setTimeout(() => location.reload(), 1500);
            } else {
                showToast(result.message || "Action failed", 'danger');
                if (currentBtn) currentBtn.disabled = false;
            }
        } catch (err) {
            console.error(err);
            showToast("Server error occurred", 'danger');
            if (currentBtn) currentBtn.disabled = false;
        }
    }

    function showToast(message, type = 'success') {
        if (!toast || !toastEl) return;
        const toastMessage = document.getElementById("toastMessage");
        if (toastMessage) toastMessage.innerText = message;

        toastEl.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-warning', 'text-bg-info');
        toastEl.classList.add(`text-bg-${type}`);
        toast.show();
    }
});

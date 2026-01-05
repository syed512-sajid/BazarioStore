// Show popup for all .coming-soon links
document.querySelectorAll(".coming-soon").forEach(link => {
    link.addEventListener("click", function (e) {
        e.preventDefault(); // prevent default navigation

        // Get modal element
        var popupModalEl = document.getElementById('popupModal');

        // Create Bootstrap modal instance
        var popupModal = new bootstrap.Modal(popupModalEl, {
            backdrop: 'static', // prevents closing when clicking outside (optional)
            keyboard: true      // allow closing with ESC key
        });

        // Show the modal
        popupModal.show();
    });
});

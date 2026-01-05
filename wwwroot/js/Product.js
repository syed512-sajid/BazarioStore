document.addEventListener("DOMContentLoaded", () => {

    const container = document.getElementById("imageContainer");
    const hiddenInput = document.getElementById("RemainingImageIds");

    function updateRemainingIds() {
        const ids = [...container.querySelectorAll(".image-box")]
            .map(div => div.dataset.id);
        hiddenInput.value = ids.join(",");
    }

    container.addEventListener("click", e => {
        if (e.target.classList.contains("remove-img")) {
            if (!confirm("Remove this image?")) return;

            e.target.closest(".image-box").remove();
            updateRemainingIds();
        }
    });

    updateRemainingIds(); // initial load
});


document.addEventListener("DOMContentLoaded", function () {

    // Disable auto sliding if only 1 image
    document.querySelectorAll('.carousel').forEach(carousel => {
        const items = carousel.querySelectorAll('.carousel-item');

        if (items.length <= 1) {
            bootstrap.Carousel.getOrCreateInstance(carousel, {
                interval: false,
                ride: false
            });
        }
    });

});

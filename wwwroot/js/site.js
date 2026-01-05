// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// untuk menu hamburger di mobile, sedang di remark ===================================
// const btn = document.getElementById("menuBtn");
// const mobile = document.getElementById("mobileMenu");

// btn.addEventListener("click", () => {
//     mobile.classList.toggle("hidden");
// });

// const btn = document.getElementById("menuBtn");
// const menu = document.getElementById("mobileMenu");
// const backdrop = document.getElementById("menuBackdrop");

// btn.addEventListener("click", () => {
//     menu.classList.remove("hidden");
//     backdrop.classList.remove("hidden");

//     // Start slide animation
//     requestAnimationFrame(() => {
//         menu.classList.add("slide-left-enter-active");
//     });
// });

// // Klik backdrop untuk menutup
// backdrop.addEventListener("click", () => {
//     menu.classList.remove("slide-left-enter-active");
//     menu.classList.add("slide-left-exit-active");

//     setTimeout(() => {
//         menu.classList.add("hidden");
//         menu.classList.remove("slide-left-exit-active");
//     }, 400);

//     backdrop.classList.add("hidden");
// });

const btn = document.getElementById("menuBtn");
const menu = document.getElementById("mobileMenu");
const backdrop = document.getElementById("menuBackdrop");

if (btn && menu && backdrop) {

    // ==== OPEN MENU ====
    btn.addEventListener("click", () => {
        menu.classList.remove("hidden");
        backdrop.classList.remove("hidden");

        // Reset exit state
        menu.classList.remove("menu-exit-active");
        backdrop.classList.remove("backdrop-hidden");

        // Mulai animasi setelah DOM apply
        requestAnimationFrame(() => {
            menu.classList.add("menu-enter-active");
            backdrop.classList.add("backdrop-visible");
        });
    });

    // ==== CLOSE MENU ====
    backdrop.addEventListener("click", () => {

        // Hapus enter-active → tambahkan exit-active (slide ke kiri)
        menu.classList.remove("menu-enter-active");
        menu.classList.add("menu-exit-active");

        backdrop.classList.remove("backdrop-visible");
        backdrop.classList.add("backdrop-hidden");

        // setelah animasi selesai → sembunyikan
        setTimeout(() => {
            menu.classList.add("hidden");
            backdrop.classList.add("hidden");

            // reset state untuk buka berikutnya
            menu.classList.remove("menu-exit-active");
        }, 300); // durasi harus sama dengan CSS transition
    });
}



// ============================================================================



// ================================
// AUTO SLIDE TO LEFT (CAROUSEL)
// ================================
function initCarousel() {
    const carousel = document.getElementById("verticalCarousel");
    const carouselAcara = document.getElementById("verticalCarouselAcara");

    if (!carousel || !carouselAcara) return;

    let autoSlideInterval = null;

    function autoSlide() {
        carousel.scrollBy({ left: 300, behavior: "smooth" });
        carouselAcara.scrollBy({ left: 300, behavior: "smooth" });

        if (carousel.scrollLeft + carousel.clientWidth >= carousel.scrollWidth - 10) {
            setTimeout(() => carousel.scrollTo({ left: 0, behavior: "smooth" }), 1000);
        }
        if (carouselAcara.scrollLeft + carouselAcara.clientWidth >= carouselAcara.scrollWidth - 10) {
            setTimeout(() => carouselAcara.scrollTo({ left: 0, behavior: "smooth" }), 1000);
        }
    }

    // Start auto slide only desktop
    function startAutoSlide() {
        if (window.innerWidth > 1024) {
            autoSlideInterval = setInterval(autoSlide, 3000);
        }
    }

    function stopAutoSlide() {
        if (autoSlideInterval) {
            clearInterval(autoSlideInterval);
            autoSlideInterval = null;
        }
    }

    startAutoSlide();

    window.addEventListener("resize", function () {
        stopAutoSlide();
        startAutoSlide();
    });

    // Button Control
    const cardWidth = 300;
    const btnPrev = document.getElementById("btnPrev");
    const btnNext = document.getElementById("btnNext");
    const btnPrevAcara = document.getElementById("btnPrevAcara");
    const btnNextAcara = document.getElementById("btnNextAcara");

    if (btnPrev) btnPrev.onclick = () => carousel.scrollLeft -= cardWidth;
    if (btnNext) btnNext.onclick = () => carousel.scrollLeft += cardWidth;

    if (btnPrevAcara) btnPrevAcara.onclick = () => carouselAcara.scrollLeft -= cardWidth;
    if (btnNextAcara) btnNextAcara.onclick = () => carouselAcara.scrollLeft += cardWidth;
}

// 🔥 Run again tiap konten berubah via HTMX
document.body.addEventListener("htmx:afterSwap", function () {
    initCarousel();
});

// 🔥 Run saat halaman pertama load
document.addEventListener("DOMContentLoaded", initCarousel);


// untuk dropdown profile di pojok kanan atas
document.addEventListener("DOMContentLoaded", () => {

    const btnprofile = document.getElementById("profileBtn");
    const menu = document.getElementById("profileMenu");

    if (btnprofile && menu) {

        btnprofile.addEventListener("click", function (e) {
            e.stopPropagation();

            // Toggle visible
            menu.classList.toggle("hidden");

            // Berikan sedikit delay supaya transition jalan
            setTimeout(() => {
                menu.classList.toggle("opacity-0");
                menu.classList.toggle("translate-y-2");
            }, 10);
        });

        // Klik di luar menutup menu
        document.addEventListener("click", function () {
            if (!menu.classList.contains("hidden")) {

                // animasi fade out
                menu.classList.add("opacity-0");
                menu.classList.add("translate-y-2");

                // setelah animasi selesai baru disembunyikan
                setTimeout(() => {
                    menu.classList.add("hidden");
                }, 200);
            }
        });
    }
});


document.body.addEventListener('htmx:beforeRequest', function (evt) {
    const mobileMenu = document.getElementById("mobileMenu");
    const backdrop = document.getElementById("menuBackdrop");

    if (evt.target.closest('#mobileMenu')) {

        // Jalankan animasi slide-out & fade
        mobileMenu.classList.add("-translate-x-full");
        backdrop.classList.add("opacity-0");

        // Setelah animasi, benar2 hide & reset class
        setTimeout(() => {
            mobileMenu.classList.add("hidden");
            backdrop.classList.add("hidden");

            // 🛠️ RESET animasi supaya ketika toggle lagi tampil normal
            mobileMenu.classList.remove("-translate-x-full");
            backdrop.classList.remove("opacity-0");
        }, 200);
    }
});

// sweeet alert ===============================
    function showSuccess(title, text) {
        Swal.fire({
            icon: 'success',
            title: title,
            text: text,
            confirmButtonColor: '#2563eb'
        });
    }

    function showError(title, text) {
        Swal.fire({
            icon: 'error',
            title: title,
            text: text,
            confirmButtonColor: '#dc2626'
        });
    }

    function showToast(type, message) {
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: type, // success | error | warning | info
            title: message,
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
        });
    }


    //Sweer alert ================================


    //Toast Notify===============================

  const notyf = new Notyf({
    duration: 4000,
    position: { x: 'right', y: 'top' },
    dismissible: true,
    ripple: true
  });

  function showSuccess(title, message) {
    notyf.success({
      message: `<strong>${title}</strong><br>${message}`,
      duration: 4000
    });
  }

  function showError(title, message) {
    notyf.error({
      message: `<strong>${title}</strong><br>${message}`,
      duration: 4000
    });
  }


  function showSuccessRedirect(title, message, redirectUrl, seconds = 4) {
    let counter = seconds;

    const toast = notyf.success({
        message: `${message}`,
        duration: seconds * 1000
    });

    const interval = setInterval(() => {
        counter--;

        if (counter <= 0) {
            clearInterval(interval);
            window.location.href = redirectUrl;
        } else {
            toast.querySelector(".notyf__message").innerHTML =
                `${message}<br><b>Redirect dalam ${counter} detik...</b>`;
        }
    }, 1000);
    
}

    //Toast Notify END===============================












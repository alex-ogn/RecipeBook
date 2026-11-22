/* =============================================================================
   home-hero.js — THE PHOTO SLIDESHOW BEHIND THE HOME PAGE BANNER
   =============================================================================
   Loaded only by Views/Home/Index.cshtml, and only when there are photos.

   WHAT IT DOES
     Every few seconds the next recipe photo fades in behind the banner text,
     and the link in the corner ("📷 recipe name") and the dots follow along.
     Clicking a dot jumps to that photo.

   HOW IT WORKS
     The view stacks all photos on top of each other. Only the photo with the
     CSS class "is-active" is visible (pages.css, .hero-slides__image.is-active);
     the fade itself is a CSS transition on "opacity". This script only moves
     the "is-active" class from one photo to the next.

   WHERE THE DATA COMES FROM (data-* attributes written by the view)
     data-hero-slides          the box holding the photos
     data-interval="6000"      milliseconds per photo (FeaturedRecipeConstants)
     data-slide-title / -url   each photo's recipe name and link
     data-slide-caption        the corner link; data-slide-caption-text = its text
     data-slide-dot="2"        a dot button and the photo number it shows

   GOOD MANNERS
     - People who asked their system to "reduce motion" still get the photos
       switching, but without the slow zoom (pages.css turns it off).
     - The slideshow pauses while something in the banner has keyboard focus
       (the search box, a button, a dot), so nobody is interrupted while typing.
   ============================================================================= */
(function () {
    const container = document.querySelector('[data-hero-slides]');
    if (!container) {
        return; // no photos on this page
    }

    const slides = Array.from(container.querySelectorAll('.hero-slides__image'));
    const dots = Array.from(document.querySelectorAll('[data-slide-dot]'));
    const caption = document.querySelector('[data-slide-caption]');
    const captionText = document.querySelector('[data-slide-caption-text]');
    const hero = container.closest('.hero');

    if (slides.length < 2) {
        return; // one photo: nothing to switch
    }

    const interval = parseInt(container.dataset.interval, 10) || 6000;

    let current = 0;      // index of the visible photo
    let timer = null;     // the id returned by setInterval, so it can be stopped

    // Shows photo number "index" and updates the caption link and the dots.
    function show(index) {
        // "%" wraps around: after the last photo comes the first one again.
        current = (index + slides.length) % slides.length;

        slides.forEach(function (slide, i) {
            slide.classList.toggle('is-active', i === current);
        });
        dots.forEach(function (dot, i) {
            dot.classList.toggle('is-active', i === current);
            // Tells screen readers which dot is the current one.
            dot.setAttribute('aria-current', i === current ? 'true' : 'false');
        });

        const slide = slides[current];
        if (caption && captionText) {
            caption.href = slide.dataset.slideUrl;
            captionText.textContent = slide.dataset.slideTitle;
        }

        // The later photos are marked loading="lazy" and only start downloading when needed.
        // Ask for the NEXT one now, so it is ready before it fades in.
        const next = slides[(current + 1) % slides.length];
        next.loading = 'eager';
    }

    function start() {
        if (timer !== null) {
            return;
        }
        timer = window.setInterval(function () {
            show(current + 1);
        }, interval);
    }

    function stop() {
        window.clearInterval(timer);
        timer = null;
    }

    // Dots: jump to that photo and restart the countdown, so it stays for a full interval.
    dots.forEach(function (dot) {
        dot.addEventListener('click', function () {
            show(parseInt(dot.dataset.slideDot, 10));
            stop();
            start();
        });
    });

    // Pause while something in the banner has keyboard focus (typing in the search box, tabbing through the dots).
    hero.addEventListener('focusin', stop);
    hero.addEventListener('focusout', start);

    show(0);
    start();
})();

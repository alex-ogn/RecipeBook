/* =============================================================================
   site.js — SMALL HELPERS LOADED ON EVERY PAGE
   =============================================================================
   Loaded by Views/Shared/_Layout.cshtml after jQuery and Bootstrap.
   Page-specific scripts live in their own files:
     recipe-form.js    the recipe create / edit form
     admin-charts.js   the admin statistics pages
     home-hero.js      the photo slideshow behind the home page banner
   ============================================================================= */

/* ---------------------------------------------------------------------------
   "Are you sure?" before sending a form
   ---------------------------------------------------------------------------
   Add a data-confirm attribute to any <form> and the browser will ask the
   question before the form is submitted:

       <form asp-action="Delete" method="post" data-confirm="Да изтрия ли коментара?">

   If the user presses "Cancel", the form is not sent.

   HOW IT WORKS: instead of attaching code to every form, we listen for the
   "submit" event once, on the whole document. Every form submission on the
   page passes through here ("event delegation"), so this also works for
   forms added to the page later.
   --------------------------------------------------------------------------- */
document.addEventListener('submit', function (event) {
    // event.target is the <form> being submitted.
    const form = event.target;

    // dataset.confirm reads the data-confirm attribute; undefined if the form has none.
    const question = form.dataset.confirm;
    if (!question) {
        return; // a normal form: do nothing, let it submit
    }

    // window.confirm shows the browser's OK / Cancel dialog and returns true for OK.
    if (!window.confirm(question)) {
        event.preventDefault(); // Cancel pressed: stop the form from being sent
    }
});

/* ---------------------------------------------------------------------------
   Sideways-scrolling rows with arrow buttons ("scroller")
   ---------------------------------------------------------------------------
   Markup (the look is in components.css, section 17):

       <div class="scroller" data-scroller>
           <button data-scroller-prev hidden>‹</button>
           <div data-scroller-track> …items in one row… </div>
           <button data-scroller-next hidden>›</button>
       </div>

   Clicking an arrow scrolls the row by most of its visible width, so the
   next tiles slide in while one or two of the old ones stay for context.
   Each arrow is hidden when the row is already at that end, and both are
   hidden when everything fits (then there is nothing to scroll).
   --------------------------------------------------------------------------- */
document.querySelectorAll('[data-scroller]').forEach(function (scroller) {
    const track = scroller.querySelector('[data-scroller-track]');
    const prev = scroller.querySelector('[data-scroller-prev]');
    const next = scroller.querySelector('[data-scroller-next]');
    if (!track || !prev || !next) {
        return;
    }

    // How far the row is scrolled, compared with how far it CAN be scrolled.
    // (The "- 1" absorbs rounding: browsers sometimes stop half a pixel short of the end.)
    function updateButtons() {
        const maxScroll = track.scrollWidth - track.clientWidth;
        prev.hidden = track.scrollLeft <= 1;
        next.hidden = track.scrollLeft >= maxScroll - 1;
    }

    function scrollByPage(direction) {
        // 80% of the visible width; "smooth" animates the movement.
        track.scrollBy({ left: direction * track.clientWidth * 0.8, behavior: 'smooth' });
    }

    prev.addEventListener('click', function () { scrollByPage(-1); });
    next.addEventListener('click', function () { scrollByPage(1); });

    // Re-check the arrows whenever the row moves (arrows, swipe, trackpad) or the window size changes.
    // "passive: true" tells the browser this listener never blocks scrolling, so scrolling stays smooth.
    track.addEventListener('scroll', updateButtons, { passive: true });
    window.addEventListener('resize', updateButtons);

    updateButtons();
});

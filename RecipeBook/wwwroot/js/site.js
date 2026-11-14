/* =============================================================================
   site.js — SMALL HELPERS LOADED ON EVERY PAGE
   =============================================================================
   Loaded by Views/Shared/_Layout.cshtml after jQuery and Bootstrap.
   Page-specific scripts live in their own files:
     recipe-form.js    the recipe create / edit form
     admin-charts.js   the admin statistics pages
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

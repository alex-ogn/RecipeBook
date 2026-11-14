/* =============================================================================
   recipe-form.js — THE RECIPE CREATE / EDIT FORM
   =============================================================================
   Loaded only by Views/Recipes/Create.cshtml and Edit.cshtml.
   The HTML it works with is in Views/Recipes/_RecipeFormFields.cshtml.

   It does two things:

   A. RICH-TEXT EDITORS
      Turns the Description and Instructions text boxes into small editors
      with bold / italic / lists (the "Trumbowyg" library).

   B. INGREDIENT PICKER
      1. Downloads the list of all ingredients from the server (once).
      2. While the user types in the search box, shows the matching ones.
      3. Clicking a match adds it to the "selected" table.
      4. "Добави нова съставка" adds an ingredient that does not exist yet.
      5. Every change is written, as JSON text, into the hidden input
         "SelectedIngredientsJson" — that is what the server receives when
         the form is submitted.

   The shape of one selected ingredient (the property names must match
   RecipeIngredientViewModel on the server):
       { ingredientId: 12, name: "Захар", quantityNeeded: "200 г", isNew: false }
   A brand-new ingredient has ingredientId 0 and isNew true.
   ============================================================================= */

// Everything is wrapped in a function that runs when the page has finished
// loading, so all the HTML elements we look up already exist.
document.addEventListener('DOMContentLoaded', function () {

    /* -----------------------------------------------------------------------
       A. RICH-TEXT EDITORS
       ----------------------------------------------------------------------- */

    // $(...) is jQuery. Trumbowyg is a jQuery plugin, so this is the one place
    // in this file that uses jQuery. Every <textarea data-rich-editor> becomes an editor.
    $('textarea[data-rich-editor]').trumbowyg({
        // The buttons in the toolbar, in groups. Keep this list in line with what the
        // server allows (RecipeHtmlSanitizer): anything else would be removed on save.
        btns: [
            ['strong', 'em', 'underline'],
            ['unorderedList', 'orderedList'],
            ['removeformat']
        ]
    });

    /* -----------------------------------------------------------------------
       B. INGREDIENT PICKER
       ----------------------------------------------------------------------- */

    // Look up the HTML elements once and keep them in variables.
    const picker = document.getElementById('ingredient-picker');
    if (!picker) {
        return; // the picker is not on this page: nothing else to do
    }

    const searchInput = document.getElementById('ingredient-search');
    const resultsList = document.getElementById('ingredient-results');
    const addNewButton = document.getElementById('add-ingredient-btn');
    const newIngredientBox = document.getElementById('new-ingredient-input');
    const newIngredientName = document.getElementById('new-ingredient-name');
    const confirmNewButton = document.getElementById('confirm-new-ingredient');
    const cancelNewButton = document.getElementById('cancel-new-ingredient');
    const selectedTableBody = document.getElementById('selected-ingredients-table');
    const hiddenJsonInput = document.getElementById('SelectedIngredientsJson');

    // All ingredients that exist in the database: [{ id: 1, name: "Брашно" }, ...].
    // Empty until the download below finishes.
    let allIngredients = [];

    // The ingredients chosen for this recipe. Starts with what the server sent:
    // the recipe's current ingredients (edit page), or what the user had picked
    // before a validation error sent the form back.
    let selectedIngredients = JSON.parse(
        document.getElementById('selected-ingredients-data').textContent || '[]');

    // --- 1. Download the list of all ingredients -------------------------------
    // The address comes from the data-ingredients-url attribute in the HTML,
    // so the script does not hard-code it.
    fetch(picker.dataset.ingredientsUrl)
        .then(function (response) { return response.json(); })
        .then(function (ingredients) { allIngredients = ingredients; })
        .catch(function () {
            // If the download fails, searching simply finds nothing;
            // the user can still add ingredients by name.
            allIngredients = [];
        });

    // --- 2. Search as the user types -------------------------------------------
    searchInput.addEventListener('input', function () {
        const query = searchInput.value.trim().toLowerCase();

        // Remove the previous results.
        resultsList.replaceChildren();

        if (query === '') {
            return;
        }

        allIngredients
            .filter(function (ingredient) {
                return ingredient.name.toLowerCase().includes(query);
            })
            .forEach(function (ingredient) {
                // Build <li><button>name</button></li>. A real <button> can be
                // reached with the Tab key and pressed with Enter.
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'ingredient-search__result';
                // textContent (not innerHTML) shows the name as plain text, so a name
                // containing "<" or "&" can never be interpreted as HTML.
                button.textContent = ingredient.name;
                button.addEventListener('click', function () {
                    addIngredient({ ingredientId: ingredient.id, name: ingredient.name, isNew: false });
                    searchInput.value = '';
                    resultsList.replaceChildren();
                    searchInput.focus();
                });

                const item = document.createElement('li');
                item.appendChild(button);
                resultsList.appendChild(item);
            });
    });

    // --- 3. Add an ingredient to the selection -----------------------------------
    function addIngredient(ingredient) {
        // An existing ingredient can be added only once.
        const alreadySelected = !ingredient.isNew && selectedIngredients.some(function (selected) {
            return selected.ingredientId === ingredient.ingredientId;
        });
        if (alreadySelected) {
            return;
        }

        selectedIngredients.push({
            ingredientId: ingredient.ingredientId,
            name: ingredient.name,
            quantityNeeded: '',
            isNew: ingredient.isNew
        });
        renderSelectedIngredients();
    }

    // --- 4. "Add a new ingredient" (one that is not in the list) -------------------
    addNewButton.addEventListener('click', function () {
        newIngredientName.value = '';
        newIngredientBox.hidden = false;   // show the small input
        newIngredientName.focus();
    });

    function confirmNewIngredient() {
        const name = newIngredientName.value.trim();
        if (name === '') {
            return;
        }
        addIngredient({ ingredientId: 0, name: name, isNew: true });
        newIngredientBox.hidden = true;
    }

    confirmNewButton.addEventListener('click', confirmNewIngredient);

    cancelNewButton.addEventListener('click', function () {
        newIngredientBox.hidden = true;
    });

    // Pressing Enter inside the "new ingredient" input adds it,
    // instead of submitting the whole recipe form by accident.
    newIngredientName.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            confirmNewIngredient();
        }
    });

    // Same for the search box: Enter must not submit the form.
    searchInput.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
        }
    });

    // --- 5. Draw the table of selected ingredients ---------------------------------
    // Called after every change: throws the old rows away and builds them again
    // from the selectedIngredients array, then updates the hidden input.
    function renderSelectedIngredients() {
        selectedTableBody.replaceChildren();

        selectedIngredients.forEach(function (ingredient, index) {
            const row = document.createElement('tr');

            // Column 1: the name (plain text).
            const nameCell = document.createElement('td');
            nameCell.textContent = ingredient.name;

            // Column 2: an input for the quantity ("200 г", "2 с.л.").
            const quantityInput = document.createElement('input');
            quantityInput.type = 'text';
            quantityInput.className = 'form-control';
            quantityInput.placeholder = 'Количество';
            quantityInput.setAttribute('aria-label', 'Количество за ' + ingredient.name);
            quantityInput.value = ingredient.quantityNeeded || '';
            // Remember the new quantity as soon as the user types it.
            quantityInput.addEventListener('input', function () {
                selectedIngredients[index].quantityNeeded = quantityInput.value;
                syncHiddenInput();
            });
            const quantityCell = document.createElement('td');
            quantityCell.appendChild(quantityInput);

            // Column 3: the remove button.
            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'btn-icon btn-icon--danger';
            removeButton.setAttribute('aria-label', 'Премахни ' + ingredient.name);
            removeButton.innerHTML = '<i class="bi bi-x-lg"></i>';   // a fixed icon, no user text
            removeButton.addEventListener('click', function () {
                selectedIngredients.splice(index, 1);   // remove 1 item at position "index"
                renderSelectedIngredients();
            });
            const removeCell = document.createElement('td');
            removeCell.className = 'data-table__actions';
            removeCell.appendChild(removeButton);

            row.append(nameCell, quantityCell, removeCell);
            selectedTableBody.appendChild(row);
        });

        syncHiddenInput();
    }

    // Writes the current selection into the hidden input as JSON text.
    function syncHiddenInput() {
        hiddenJsonInput.value = JSON.stringify(selectedIngredients);
    }

    // Show the starting selection when the page opens.
    renderSelectedIngredients();
});

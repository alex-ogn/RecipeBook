﻿using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeFilterViewModel
    {
        public IEnumerable<Recipe> Recipes { get; set; }
        public SelectList Categories { get; set; }
        public int? SelectedCategoryId { get; set; }
    }
}

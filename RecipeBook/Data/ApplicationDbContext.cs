using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Models;

namespace RecipeBook.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<IngredientCategory> IngredientCategories { get; set; }
        public DbSet<Recipe> Recipies { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<RecipeCategory> RecipeCategories { get; set; }
        public DbSet<SavedRecipe> SavedRecipes { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RecipeIngredient: НЕкаскадно
            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Recipe → Category: НЕкаскадно
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.Category)
                .WithMany()
                .HasForeignKey("RecipeCategoryId")
                .OnDelete(DeleteBehavior.Restrict);

            // Ingredient → Category: НЕкаскадно
            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey("IngredientCategoryId")
                .OnDelete(DeleteBehavior.Restrict);

            //// SavedRecipe: 
            modelBuilder.Entity<SavedRecipe>()
                .HasOne(sr => sr.User)
                .WithMany(u => u.SavedRecipes)
                .HasForeignKey(sr => sr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavedRecipe>()
                .HasOne(sr => sr.Recipe)
                .WithMany(r => r.SavedByUsers)
                .HasForeignKey(sr => sr.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
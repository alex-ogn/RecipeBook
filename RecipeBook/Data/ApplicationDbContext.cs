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
        public DbSet<UserFollower> UserFollowers { get; set; }
        public DbSet<RecipeLike> RecipeLikes { get; set; }
        public DbSet<RecipeComment> RecipeComments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // unique indexes
            modelBuilder.Entity<IngredientCategory>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<RecipeCategory>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Ingredient>()
                .HasIndex(i => new { i.Name, i.IngredientCategoryId })
                .IsUnique();

            // RecipeIngredient
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

            // Recipe → Category
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.RecipeCategory)
                .WithMany(c => c.Recipes)
                .HasForeignKey("RecipeCategoryId")
                .OnDelete(DeleteBehavior.Restrict);

            // Ingredient → Category
            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.IngredientCategory)
                .WithMany(c => c.Ingredients)
                .HasForeignKey(i => i.IngredientCategoryId)
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

            // UserFollower
            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Followed)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowedId)
                .OnDelete(DeleteBehavior.Restrict);

            // RecipeLike
            modelBuilder.Entity<RecipeLike>()
                .HasOne(l => l.User)
                .WithMany(u => u.LikedRecipes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecipeLike>()
                .HasOne(l => l.Recipe)
                .WithMany(r => r.Likes)
                .HasForeignKey(l => l.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            // RecipeComment
            modelBuilder.Entity<RecipeComment>()
                .HasOne(rc => rc.Recipe)
                .WithMany(r => r.Comments)
                .HasForeignKey(rc => rc.RecipeId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<RecipeComment>()
                .HasOne(rc => rc.User)
                .WithMany()
                .HasForeignKey(rc => rc.UserId)
                .OnDelete(DeleteBehavior.Restrict); 


        }
    }
}
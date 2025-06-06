using Microsoft.AspNetCore.Identity;
using RecipeBook.Models;

namespace RecipeBook.Data
{
    public static class DataSeeder
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                context.Database.EnsureCreated();

                // Seed roles
                SeedRoles(roleManager).Wait();

                // Seed users
                SeedUsers(userManager).Wait();
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
        }

        private static async Task SeedUsers(UserManager<ApplicationUser> userManager)
        {
            if (userManager.FindByEmailAsync("admin@example.com").Result == null)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@example.com"
                };

                IdentityResult result = userManager.CreateAsync(user, "Admin123!").Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Admin").Wait();
                }
            }
        }
    }

}

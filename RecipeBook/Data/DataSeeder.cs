using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Options;

namespace RecipeBook.Data
{
    /// <summary>
    /// Class for seeding initial roles, the administrator profile and required reference data safely.
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Asynchronously initializes admin profile and basic roles using IConfiguration for sensitive data.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DataSeeder");

            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedRolesAsync(roleManager, logger);
            await SeedUsersAsync(userManager, configuration, logger);
            await SeedReferenceDataAsync(context, logger);
        }

        /// <summary>
        /// Data the app cannot work without. Safe to run on every start: it only adds what is missing.
        /// </summary>
        private static async Task SeedReferenceDataAsync(ApplicationDbContext context, ILogger logger)
        {
            // Recipe forms put ingredients typed by users into this category (see RecipeService).
            var categoryName = IngredientCategoryConstants.AddedByUserCategoryName;
            if (!await context.IngredientCategories.AnyAsync(c => c.Name == categoryName))
            {
                context.IngredientCategories.Add(new IngredientCategory { Name = categoryName });
                await context.SaveChangesAsync();
                logger.LogInformation("Created the '{Category}' ingredient category.", categoryName);
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roles = { UserRoles.Admin, UserRoles.User };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!result.Succeeded)
                    {
                        logger.LogError("Failed to create role '{RoleName}': {Errors}",
                            roleName, string.Join(", ", result.Errors));
                    }
                }
            }
        }

        private static async Task SeedUsersAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            // IConfiguration (User Secrets / Environment Variables)
            var credentials = configuration.GetSection(AdminCredentials.SectionName).Get<AdminCredentials>();
            var adminEmail = credentials?.Email;
            var adminPassword = credentials?.Password;

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Admin credentials are not set in IConfiguration. Skipping admin seeding.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (createResult.Succeeded)
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError("Failed to assign '{Role}' role to user '{Email}': {Errors}",
                            UserRoles.Admin, adminEmail, string.Join(", ", roleResult.Errors));
                    }
                }
                else
                {
                    logger.LogError("Failed to create admin user '{Email}': {Errors}",
                        adminEmail, string.Join(", ", createResult.Errors));
                }
            }
        }
    }
}
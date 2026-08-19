using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecipeBook.Models;
using System;
using System.Threading.Tasks;

namespace RecipeBook.Data
{
    /// <summary>
    /// Class for seeding initial roles and the administrator profile safely.
    /// </summary>
    public static class DataSeeder
    {
        public const string AdminRole = "Admin";
        public const string UserRole = "User";

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

            await SeedRolesAsync(roleManager, logger);
            await SeedUsersAsync(userManager, configuration, logger);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roles = { AdminRole, UserRole };

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
            var adminEmail = configuration["AdminCredentials:Email"];
            var adminPassword = configuration["AdminCredentials:Password"];

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
                    var roleResult = await userManager.AddToRoleAsync(adminUser, AdminRole);
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError("Failed to assign '{Role}' role to user '{Email}': {Errors}",
                            AdminRole, adminEmail, string.Join(", ", roleResult.Errors));
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
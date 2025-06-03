using Microsoft.AspNetCore.Identity;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;

namespace RecipeBook.Services
{
    public interface IUserProfileService
    {
        Task<IdentityResult> UpdateUserProfileAsync(ApplicationUser user, EditUserViewModel model, bool isAdmin = false);
    }
}

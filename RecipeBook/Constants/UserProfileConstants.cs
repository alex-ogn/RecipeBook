using System.Net.Mime;

namespace RecipeBook.Constants
{
    public static class UserProfileConstants
    {
        public const int MinUserNameLength = 2;
        public const string DefaultProfilePictureName = "default-profile.png";
        public const string DefaultProfilePictureFormat = MediaTypeNames.Image.Png;
        public const string PhoneNumberRegexPattern = @"^(\+359|0)?8[7-9][0-9]{7}$";
    }
}

using System.Text.RegularExpressions;

namespace RecipeBook.Constants
{
    public static class UserProfileConstants
    {
        public const int MinUserNameLength = 2;
        public const String DefaultProfilePictureName = "default-profile.png";
        public const String DefaultProfilePictureFormat = "image/png";
        public const String PhoneNumberRegexPattern = @"^(\+359|0)?8[7-9][0-9]{7}$";


    }
}

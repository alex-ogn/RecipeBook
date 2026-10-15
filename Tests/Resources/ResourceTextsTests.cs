using System.ComponentModel.DataAnnotations;
using System.Reflection;
using RecipeBook.Resources;
using RecipeBook.ViewModels.Recipes;

namespace Tests.Resources
{
    public class ResourceTextsTests
    {
        public static TheoryData<Type> ResourceTypes => new()
        {
            typeof(DisplayTexts),
            typeof(ValidationTexts),
            typeof(MessageTexts)
        };

        [Theory]
        [MemberData(nameof(ResourceTypes))]
        public void AllKeys_ResolveToText(Type resourceType)
        {
            var properties = resourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string));

            foreach (var property in properties)
            {
                var value = (string?)property.GetValue(null);
                Assert.False(string.IsNullOrWhiteSpace(value), $"{resourceType.Name}.{property.Name} is missing");
            }
        }

        [Fact]
        public void ValidationMessages_UseAttributeLimits()
        {
            var model = new RecipeEditViewModel { Title = string.Empty, Instructions = "Cook", Servings = 500, CookingTime = 10 };
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

            var servingsError = Assert.Single(results, r => r.MemberNames.Contains(nameof(RecipeEditViewModel.Servings)));
            Assert.Equal(string.Format(ValidationTexts.ServingsRange, DisplayTexts.Servings, 1, 100), servingsError.ErrorMessage);
            Assert.Contains("1", servingsError.ErrorMessage);
            Assert.Contains("100", servingsError.ErrorMessage);
        }

        [Fact]
        public void DisplayNames_ComeFromResources()
        {
            var display = typeof(RecipeEditViewModel)
                .GetProperty(nameof(RecipeEditViewModel.Title))!
                .GetCustomAttribute<DisplayAttribute>()!;

            Assert.Equal(DisplayTexts.Title, display.GetName());
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace Tests.Data
{
    public class ApplicationDbContextModelTests
    {
        [Fact]
        public void RecipeTable_KeepsItsExistingName()
        {
            // The SQL Server model is built without opening a connection.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=unused;Database=unused")
                .Options;
            using var context = new ApplicationDbContext(options);

            var tableName = context.Model.FindEntityType(typeof(Recipe))!.GetTableName();

            Assert.Equal("Recipies", tableName);
        }
    }
}

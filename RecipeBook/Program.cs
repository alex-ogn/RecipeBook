using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Options;
using RecipeBook.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<CustomIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
// Every unsafe request (POST/PUT/DELETE) must carry an antiforgery token; tag-helper forms add it automatically.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// custom services
builder.Services.AddScoped<IRecipePdfService, RecipePdfService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IRecipeCategoryService, RecipeCategoryService>();
builder.Services.AddScoped<IIngredientCategoryService, IngredientCategoryService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IRecipeInteractionService, RecipeInteractionService>();
builder.Services.AddSingleton(RecipeHtmlSanitizer.Create());
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IdentityErrorDescriber, CustomIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Users/AccessDenied";
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

// Configure email sender based on the environment
if (builder.Environment.IsDevelopment())
{
    // In development, use FileEmailSender to write emails to a file for testing
    builder.Services.AddTransient<IEmailSender, FileEmailSender>();
}
else
{
    // In production, use EmailSender to send real emails
    builder.Services.AddTransient<IEmailSender, EmailSender>();
}

var app = builder.Build();

var supportedCultures = new[] { "bg", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("bg")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/Identity/Account/Login", context =>
{
    context.Response.Redirect("/Account/Login");
    return Task.CompletedTask;
});

app.MapGet("/Identity/Account/Register", context =>
{
    context.Response.Redirect("/Account/Register");
    return Task.CompletedTask;
});

app.MapGet("/Identity/Account/Manage/{*path}", context =>
{
    context.Response.Redirect("/Users/Edit");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seed the database with the custom DataSeeder class
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        await DataSeeder.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();

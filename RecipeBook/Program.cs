using System.Net.Mime;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Options;
using RecipeBook.Resources;
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

        // Lock the account after repeated wrong passwords (brute-force protection).
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = AccountSecurityConstants.MaxFailedLoginAttempts;
        options.Lockout.DefaultLockoutTimeSpan = AccountSecurityConstants.LockoutDuration;
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

// Limit how often one client can post the login and registration forms.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(AccountSecurityConstants.AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AccountSecurityConstants.AuthRequestsPerWindow,
                Window = AccountSecurityConstants.AuthRateLimitWindow,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = $"{MediaTypeNames.Text.Plain}; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(MessageTexts.TooManyRequests, cancellationToken);
    };
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
app.UseRateLimiter();

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
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            // Integration tests replace SQL Server with an in-memory database, which has no migrations.
            await context.Database.EnsureCreatedAsync();
        }

        await DataSeeder.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();

/// <summary>
/// Exposes the entry point to the integration tests (WebApplicationFactory&lt;Program&gt;).
/// </summary>
public partial class Program { }

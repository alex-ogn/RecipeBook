using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<CustomIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddControllersWithViews();

 // custom services
builder.Services.AddScoped<IRecipePdfService, RecipePdfService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Users/AccessDenied";
});

var app = builder.Build();

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
DataSeeder.Initialize(app.Services);

app.Run();

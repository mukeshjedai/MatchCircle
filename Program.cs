using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using testapp1.Data;
using testapp1.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Configure file upload limits
    options.MaxModelBindingCollectionSize = 10;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB total
    options.ValueLengthLimit = 5 * 1024 * 1024; // 5MB per file
    options.MultipartBoundaryLengthLimit = int.MaxValue;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

// Configure persistent session with longer timeout
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30); // 30 days
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".MatchCircle.Session";
});

// Configure Authentication with persistent cookies
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // 30 days persistent
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".MatchCircle.Auth";
    options.Cookie.IsEssential = true;
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    }
    else
    {
        // Use placeholder values - user needs to configure in appsettings.json
        options.ClientId = "YOUR_GOOGLE_CLIENT_ID";
        options.ClientSecret = "YOUR_GOOGLE_CLIENT_SECRET";
    }
    
    // Set callback path - this must match what's registered in Google Cloud Console
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    
    // Configure redirect URI to handle both localhost and 127.0.0.1
    // The actual redirect URI will be constructed from the request
});

// Add PostgreSQL database context
builder.Services.AddDbContext<MatrimonialDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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
app.UseSession();
app.UseSessionRestore();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed test user in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MatrimonialDbContext>();
        try
        {
            await DbSeeder.SeedTestUserAsync(context);
            await DbSeeder.SeedKaliUserAndMessagesAsync(context);
        }
        catch (Exception ex)
        {
            // Log error but don't stop application startup
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

app.Run();

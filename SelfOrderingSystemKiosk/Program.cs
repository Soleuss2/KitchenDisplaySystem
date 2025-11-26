using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SelfOrderingSystemKiosk.Areas.Admin.Models;
using SelfOrderingSystemKiosk.Models;
using SelfOrderingSystemKiosk.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

// Bind all settings classes
builder.Services.Configure<DataConSettings>(
    builder.Configuration.GetSection("DataCon"));

builder.Services.Configure<MongoDBSettings>(options =>
{
    // Bind Kitchen settings 
    builder.Configuration.GetSection("KitchenDatabase").Bind(options);

    // Connection string with DataCon's value
    options.ConnectionString = builder.Configuration["DataCon:ConnectionString"];
});

builder.Services.Configure<AuthenticationSettings>(
    builder.Configuration.GetSection("Authentication"));

// ------------------
// MongoDB DI Setup
// ------------------

// Register a single IMongoClient for the app
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config["DataCon:ConnectionString"];
    return new MongoClient(connectionString);
});

// Register services that use IMongoClient instead of IMongoDatabase
// Each service gets the database itself internally
builder.Services.AddSingleton<StockService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<AuthService>();

// Other services
builder.Services.AddSingleton<KitchenDatabase>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddScoped<ChickenService>();

builder.Services.AddSession();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    });
// ✅ Enable Session Support (future use)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var config = sp.GetRequiredService<IConfiguration>();
    var authDbName = config["Authentication:DatabaseName"] ?? "Users";
    return client.GetDatabase(authDbName);
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Enable session
app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Admin}/{controller=Dashboard}/{action=Index}/{id?}");


app.Run();
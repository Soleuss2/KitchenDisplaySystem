
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
    // Bind Kitchen settings first
    builder.Configuration.GetSection("KitchenDatabase").Bind(options);

    // Then override the connection string with DataCon's value
    options.ConnectionString = builder.Configuration["DataCon:ConnectionString"];
});

builder.Services.Configure<AuthenticationSettings>(
    builder.Configuration.GetSection("Authentication"));


// Register IMongoDatabase (Authentication database)
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var config = builder.Configuration;

    var mongoClient = new MongoClient(config["DataCon:ConnectionString"]);

    // This is your Users DB – keep it as is
    return mongoClient.GetDatabase(config["Authentication:DatabaseName"]);
});


builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<KitchenDatabase>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddScoped<ChickenService>();
builder.Services.AddSingleton<AuthService>();

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
    pattern: "{area=Admin}/{controller=Account}/{action=Login}/{id?}");


app.Run();
    
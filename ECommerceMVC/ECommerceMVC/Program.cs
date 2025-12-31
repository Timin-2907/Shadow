using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== Services =====
builder.Services.AddControllersWithViews();

// ✅ THÊM ANTIFORGERY
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Database
builder.Services.AddDbContext<ShoeContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Shoe")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/KhachHang/DangNhap";
        options.AccessDeniedPath = "/AccessDenied";
    });

// PayPal
var paypalClientId = builder.Configuration["PaypalOptions:AppId"];
var paypalSecret = builder.Configuration["PaypalOptions:AppSecret"];
var paypalMode = builder.Configuration["PaypalOptions:Mode"] ?? "sandbox";

builder.Services.AddSingleton(x =>
    new PaypalClient(paypalClientId ?? "", paypalSecret ?? "", paypalMode)
);

// VNPay
builder.Services.AddSingleton<IVnPayService, VnPayService>();

// SEO / Email / Newsletter
builder.Services.AddScoped<ISEOService, SEOService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

var app = builder.Build();

// ===== Middleware =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ===== Routes =====
app.MapControllerRoute(
    name: "product-detail",
    pattern: "san-pham/{slug}-{id:int}",
    defaults: new { controller = "HangHoa", action = "Detail" });

app.MapControllerRoute(
    name: "category",
    pattern: "danh-muc/{slug}-{id:int}",
    defaults: new { controller = "HangHoa", action = "Category" });

app.MapControllerRoute(
    name: "adminOrder",
    pattern: "Admin/Orders/{action=Index}/{id?}",
    defaults: new { controller = "AdminOrder" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
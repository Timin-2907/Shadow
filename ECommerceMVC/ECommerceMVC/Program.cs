using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Add services to container
// =======================
builder.Services.AddControllersWithViews();

// ===== Database =====
builder.Services.AddDbContext<ShoeContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Shoe"));
});

// ===== Session =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== AutoMapper =====
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// ===== Authentication =====
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/KhachHang/DangNhap";
    options.AccessDeniedPath = "/AccessDenied";
});

// ===== PayPal =====
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"],
    builder.Configuration["PaypalOptions:AppSecret"],
    builder.Configuration["PaypalOptions:Mode"]
));

// ===== VNPay =====
builder.Services.AddSingleton<IVnPayService, VnPayService>();

// ===== SEO =====
builder.Services.AddScoped<ISEOService, SEOService>();

// ===== Email =====
builder.Services.AddScoped<IEmailService, EmailService>();

// ===== Newsletter (THÊM MỚI) =====
builder.Services.AddScoped<INewsletterService, NewsletterService>();

var app = builder.Build();

// =======================
// Configure middleware
// =======================
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

// =======================
// Routes
// =======================

// Product detail (SEO)
app.MapControllerRoute(
    name: "product-detail",
    pattern: "san-pham/{slug}-{id:int}",
    defaults: new { controller = "HangHoa", action = "Detail" });

// Category
app.MapControllerRoute(
    name: "category",
    pattern: "danh-muc/{slug}-{id:int}",
    defaults: new { controller = "HangHoa", action = "Category" });

// Admin order
app.MapControllerRoute(
    name: "adminOrder",
    pattern: "Admin/Orders/{action=Index}/{id?}",
    defaults: new { controller = "AdminOrder" });

// Default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

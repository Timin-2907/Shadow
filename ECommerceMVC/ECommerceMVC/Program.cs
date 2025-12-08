using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ShoeContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Shoe"));
});

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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/KhachHang/DangNhap";
    options.AccessDeniedPath = "/AccessDenied";
});

// PayPal
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOptions:AppId"],
    builder.Configuration["PaypalOptions:AppSecret"],
    builder.Configuration["PaypalOptions:Mode"]
));

// VNPay
builder.Services.AddSingleton<IVnPayService, VnPayService>();

// SEO Service (THÊM MỚI)
builder.Services.AddScoped<ISEOService, SEOService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// SEO-friendly routes (THÊM MỚI - ĐẶT TRƯỚC default route)
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

// Default route (GIỮ NGUYÊN Ở CUỐI)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
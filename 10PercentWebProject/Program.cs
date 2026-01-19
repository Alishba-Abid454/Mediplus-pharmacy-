using _10PercentWebProject.Data;
using _10PercentWebProject.Hubs;
using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using _10PercentWebProject.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>(); 
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IAdminMedicineRepository, AdminMedicineRepository>();

builder.Services.AddSignalR();
//session 
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ? Add HttpContextAccessor (IMPORTANT for repositories)
builder.Services.AddHttpContextAccessor();

//Authorization Policies

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireClaim("Role", "Admin"));
    
    options.AddPolicy("UserPolicy", policy =>
        policy.RequireClaim("Role", "User"));
});
var app = builder.Build();

app.UseSession();   
app.MapRazorPages();

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

app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/Identity/Account/Register", permanent: false);
        return;
    }
    await next();
});

app.MapHub<InventoryHub>("/inventoryHub");
app.MapHub<OrderHub>("/orderHub");

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action}/{id?}",
    defaults: new { controller = "Admin" });


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

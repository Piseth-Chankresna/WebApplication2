using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

var builder = WebApplication.CreateBuilder(args);

// ១. កំណត់ការប្រើប្រាស់ Database (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ២. បន្ថែម Services សម្រាប់ MVC (Controllers with Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ៣. កំណត់ការបង្ហាញ Error ទៅតាម Environment
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ៤. កំណត់ Route លំនាំដើម (Default Route)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
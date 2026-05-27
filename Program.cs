using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=ollinbarber.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    db.Database.Migrate();
    NormalizarConfiguracionSistema(db);

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in new[] { "Admin", "Barbero" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@ollin.com";
    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        var user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nombre = "Administrador",
            Celular = "3000000000",
            EsBarbero = false,
            Disponible = true
        };

        await userManager.CreateAsync(user, "123456");
        await userManager.AddToRoleAsync(user, "Admin");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

static void NormalizarConfiguracionSistema(ApplicationDbContext db)
{
    var configuraciones = db.ConfiguracionSistema
        .OrderByDescending(c => c.Id)
        .ToList();

    if (!configuraciones.Any())
    {
        db.ConfiguracionSistema.Add(new ConfiguracionSistema());
        db.SaveChanges();
        return;
    }

    var duplicadas = configuraciones.Skip(1).ToList();

    if (duplicadas.Any())
    {
        db.ConfiguracionSistema.RemoveRange(duplicadas);
        db.SaveChanges();
    }
}

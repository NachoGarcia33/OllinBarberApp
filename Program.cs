using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;
using System.Globalization;
using Microsoft.AspNetCore.Localization;


AppContext.SetSwitch(
    "Npgsql.EnableLegacyTimestampBehavior",
    true
);

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Contraseñas
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Bloqueo por intentos fallidos
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
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

    try
    {
        Console.WriteLine("Aplicando migraciones...");

        await db.Database.MigrateAsync();

        NormalizarConfiguracionSistema(db);

        Console.WriteLine("Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
        throw;
    }

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in new[] { "Admin", "Barbero" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail =
    Environment.GetEnvironmentVariable("ADMIN_EMAIL")
    ?? "admin@ollinbarber.com";
    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        var user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nombre = "Pedro Hernandez",
            Celular = "3045580585",
            EsBarbero = false,
            Disponible = true
        };

        var adminPassword =
            Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? "Cambiar123!";
        var result = await userManager.CreateAsync(
    user,
    adminPassword
);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
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

var cultura = new CultureInfo("es-CO");

CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

app.UseRequestLocalization(
    new RequestLocalizationOptions
    {
        DefaultRequestCulture =
            new RequestCulture("es-CO"),

        SupportedCultures =
            new[] { cultura },

        SupportedUICultures =
            new[] { cultura }
    });

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

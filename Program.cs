using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using GestionCreditos.Data;
using GestionCreditos.Hubs;
using GestionCreditos.Infrastructure;
using GestionCreditos.Services.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificacionPublisher, RabbitMqNotificacionPublisher>();
builder.Services.AddHostedService<NotificacionConsumerService>();

// Caché distribuida basada en Redis (configurable en appsettings), con respaldo en memoria
// para que la aplicación siga funcionando si Redis no está disponible.
builder.Services.AddSingleton<IDistributedCache>(sp =>
{
    var redisConfiguration = builder.Configuration["Redis:Configuration"] ?? "localhost:6379";
    var redis = new RedisCache(new RedisCacheOptions
    {
        Configuration = redisConfiguration,
        InstanceName = "GestionCreditos:"
    });
    var memoria = new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()),
        sp.GetRequiredService<ILoggerFactory>());
    return new ResilientDistributedCache(redis, memoria);
});

// Sesión (respaldada por el caché distribuido anterior).
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthentication();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<SolicitudesHub>("/hubs/solicitudes");

app.MapRazorPages()
   .WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    await DbInitializer.InitializeAsync(app.Services);
}

app.Run();

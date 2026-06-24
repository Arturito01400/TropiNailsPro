using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Services;
using TropiNailsPro.Hubs;
using TropiNailsPro.Middlewares;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// MYSQL
var connectionString =
    builder.Configuration.GetConnectionString("ConexionMysql");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                5,
                TimeSpan.FromSeconds(10),
                null
            );
        }
    );
});

// MVC + JSON
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "Este campo es obligatorio.");
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024 * 50;
});

// ================================
// SERVICIOS
// ================================
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<PayPalService>();
builder.Services.AddScoped<TimeService>();

builder.Services.AddHttpContextAccessor();

// AUTH (SIN CAMBIOS ESTRUCTURALES)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// SESIÓN (CORREGIDO SOLO PARA AZURE SEGURIDAD)
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // ✔ ESTABLE Y COMPATIBLE CON AZURE
    options.Cookie.SameSite = SameSiteMode.None;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// CULTURA
var supportedCultures = new[]
{
    new CultureInfo("es-DO"),
    new CultureInfo("en-US")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-DO");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// SUBIDAS
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000;
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

// SERVICIOS RED SOCIAL
builder.Services.AddScoped<PublicacionService>();
builder.Services.AddScoped<ComentarioService>();
builder.Services.AddScoped<LikeService>();

var app = builder.Build();

// EF MIGRATIONS
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERROR EF CORE");
        Console.WriteLine(ex.Message);
    }
}

// FFMPEG
var ffmpegFolder = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");
var ffmpegExe = Path.Combine(ffmpegFolder, "ffmpeg.exe");

if (!Directory.Exists(ffmpegFolder) || !File.Exists(ffmpegExe))
{
    try
    {
        FFmpegDownloader.GetLatestVersion(
            FFmpegVersion.Official,
            ffmpegFolder
        ).GetAwaiter().GetResult();
    }
    catch { }
}

FFmpeg.SetExecutablesPath(ffmpegFolder);

// PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ❌ ELIMINADO: UseCookiePolicy (ROMPE LOGIN EN AZURE)

// STATIC FILES
app.UseStaticFiles();

// UPLOADS
var uploadsPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "wwwroot",
    "uploads"
);

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "uploads")
    ),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseRequestLocalization();

// 🔥 ORDEN CORRECTO Y SEGURO EN AZURE
app.UseAuthentication();
app.UseSession();

app.UseAuthorization();

app.UseMiddleware<SuscripcionMiddleware>();

// RUTAS
app.MapControllerRoute(
    name: "notificaciones",
    pattern: "Notificaciones/{action=Index}/{id?}",
    defaults: new { controller = "Notificaciones", action = "Index" });

app.MapControllerRoute(
    name: "manicurista_slug",
    pattern: "{slug}",
    defaults: new { controller = "Salones", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// HUBS
app.MapHub<OnlineHub>("/onlineHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<AvatarHub>("/avatarHub");
app.MapHub<CallHub>("/callHub");

app.Run();
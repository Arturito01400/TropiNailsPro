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

        // 🔥 FIX MYSQL RETRY
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

// MVC
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "Este campo es obligatorio.");
});

// =============================================
// 🔥 JSON FIX GLOBAL
// =============================================

builder.Services.AddControllers()
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
    // 🔥 FIX TIEMPO REAL
    options.EnableDetailedErrors = true;

    options.MaximumReceiveMessageSize = 1024 * 1024 * 50;
});

// Servicios
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PayPalService>();

// =====================================================
// 🔥 AUTH
// =====================================================

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";

        options.AccessDeniedPath = "/Auth/Login";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;

        // 🔥 SEGURIDAD EXTRA
        options.Cookie.HttpOnly = true;

        // 🔥 FIX LOCALHOST + PRODUCCIÓN
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
            ?
            CookieSecurePolicy.SameAsRequest
            :
            CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// =====================================================
// 🔥 SESIÓN
// =====================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // 🔥 FIX AZURE (sin romper local)
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Cultura
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

// Subidas grandes
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000;

    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

// Servicios red social
builder.Services.AddScoped<PublicacionService>();
builder.Services.AddScoped<ComentarioService>();
builder.Services.AddScoped<LikeService>();

var app = builder.Build();

// =============================================
// 🔥 FIX GLOBAL EF CORE / MYSQL
// =============================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 🔥 MIGRACIONES AUTOMÁTICAS
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("======================================");

        Console.WriteLine("ERROR DE ENTITY FRAMEWORK");

        Console.WriteLine(ex.Message);

        Console.WriteLine("======================================");
    }
}

// =============================================
// 🔥 FFMPEG
// =============================================

var ffmpegFolder = Path.Combine(
    Directory.GetCurrentDirectory(),
    "ffmpeg"
);

var ffmpegExe = Path.Combine(
    ffmpegFolder,
    "ffmpeg.exe"
);

if (!Directory.Exists(ffmpegFolder) || !File.Exists(ffmpegExe))
{
    try
    {
        FFmpegDownloader.GetLatestVersion(
            FFmpegVersion.Official,
            ffmpegFolder
        ).GetAwaiter().GetResult();
    }
    catch
    {
    }
}

FFmpeg.SetExecutablesPath(ffmpegFolder);

// =============================================
// 🔥 PIPELINE
// =============================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None
});

// =============================================
// 🔥 STATIC FILES
// =============================================

app.UseStaticFiles();

// =============================================
// 🔥 ASEGURAR CARPETA uploads
// =============================================

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
        Path.Combine(
            builder.Environment.WebRootPath,
            "uploads"
        )
    ),

    RequestPath = "/uploads"
});

app.UseRouting();

app.UseRequestLocalization();

// =============================================
// 🔥 ORDEN CRÍTICO
// =============================================

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<SuscripcionMiddleware>();

// =============================================
// 🔥 RUTAS
// =============================================

app.MapControllerRoute(
    name: "notificaciones",
    pattern: "Notificaciones/{action=Index}/{id?}",
    defaults: new
    {
        controller = "Notificaciones",
        action = "Index"
    });

app.MapControllerRoute(
    name: "public",
    pattern: "{controller}/{action}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "public",
    pattern: "{controller}/{action}/{id?}");

app.MapControllerRoute(
    name: "manicurista_slug",
    pattern: "{slug}",
    defaults: new
    {
        controller = "Salones",
        action = "Index"
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// =============================================
// 🔥 HUBS
// =============================================

app.MapHub<OnlineHub>("/onlineHub");

app.MapHub<ChatHub>("/chatHub");

app.MapHub<NotificationHub>("/notificationHub");

app.MapHub<AvatarHub>("/avatarHub");

app.MapHub<CallHub>("/callHub");

app.Run();

// db.Database.Migrate();
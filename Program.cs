using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.AddRazorPages(); // tanpa aut
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
}).AddMvcOptions(options =>
{
    options.Filters.Add<TestLandingPageNet8.Helpers.VendorAccessFilter>();
});

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>(); // Registrasi DbConnectionFactory sebagai singleton database v1

builder.Services.AddScoped<EmailService>(); // Service untuk email

builder.Services.AddHttpClient();

builder.Services.AddSession(); // untuk aktifkan session


// 🔐 Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";          // kalau belum login
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";

        //untuk Dev gunakan ini dulu karena cookie butuh secure di https

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Sesuaikan dengan request (HTTP atau HTTPS) bisa dengan http ataupun https untuk browser di hp
                                                                        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Hanya kirim cookie melalui HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;


        options.ExpireTimeSpan = TimeSpan.FromDays(30); //untuk set berapa lama cookie akan di simpan, bisa detik, menit, jam ,dan hari
        options.SlidingExpiration = true;
    });


// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

Db.Configure(builder.Configuration); // Inisialisasi koneksi database V2 agar pemanggilan lebih simple di Db.Connect()

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Aktifkan Session


app.UseAuthentication(); // Aktifkan Authentication
app.UseAuthorization();

// 🚨 --- AWAL GLOBAL MIDDLEWARE PENJAGA SECURITY ---
app.Use(async (context, next) =>
{
    var user = context.User;

    // Cek jika user berhasil terautentikasi (sudah login)
    if (user.Identity?.IsAuthenticated == true)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value?.ToUpper();

        // Jika akun yang masuk adalah SECURITY atau SICURITY
        if (role == "SECURITY" || role == "SICURITY")
        {
            // Ambil objek PathString langsung (Jangan gunakan .Value dan jangan di-set string kosong "")
            PathString requestPath = context.Request.Path;

            // Berikan izin akses HANYA untuk halaman PelanggaranInput, proses upload, request log out, dan aset CSS/JS static web.
            bool isAllowedPath = requestPath.Value.Contains("/PelanggaranInput/PelanggaranInput", StringComparison.OrdinalIgnoreCase) ||
                                 requestPath.Value.Contains("/Logout", StringComparison.OrdinalIgnoreCase) ||
                                 requestPath.Value.Contains("/uploads/", StringComparison.OrdinalIgnoreCase) ||
                                 requestPath.StartsWithSegments("/_framework") ||
                                 requestPath.StartsWithSegments("/_blazor");

            // Jika dia mencoba mengetik url halaman lain (Index, Billing, dll), paksa tendang balik!
            if (!isAllowedPath)
            {
                context.Response.Redirect("/PelanggaranInput/PelanggaranInput");
                return; // Memutus pipeline agar halaman lain tidak sempat dieksekusi
            }
        }
    }

    await next();
});
// 🚨 --- AKHIR GLOBAL MIDDLEWARE PENJAGA SECURITY ---

app.MapRazorPages();

// Tambahkan konfigurasi ini untuk mendengarkan di semua IP di port 5134
// app.Urls.Add("http://0.0.0.0:5134"); 

app.Run();

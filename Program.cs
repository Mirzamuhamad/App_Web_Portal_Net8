using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.AddRazorPages(); // tanpa aut
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>(); // Registrasi DbConnectionFactory sebagai singleton database v1

builder.Services.AddScoped<EmailService>(); // Service untuk email

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

app.MapRazorPages();

// Tambahkan konfigurasi ini untuk mendengarkan di semua IP di port 5134
app.Urls.Add("http://0.0.0.0:5134"); 

app.Run();

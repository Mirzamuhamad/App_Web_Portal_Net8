using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;




public class LoginModel : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public LoginInputModel InputLogin { get; set; } = new();


    private readonly IConfiguration _config;
    private readonly EmailService _email; //untuk email

    public LoginModel(IConfiguration config, EmailService email)
    {
        _config = config;
        _email = email; //untuk email
    }

    // public void OnGet()
    // {

    // }

    public IActionResult OnGet()
    {
        //  Jika sudah login cek cookie apakah sudah isi atau belum, jika sudah jangan lagi masuk ke halaman login lagi
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index"); // atau /Dashboard
        }
        return Page();
    }


    // ================= Login =================//
    public async Task<IActionResult> OnPostLogin()
    {
        // 🔑 VALIDASI KHUSUS LOGIN SAJA
        ModelState.Clear();

        if (!TryValidateModel(InputLogin, nameof(InputLogin)))
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Any())
                .ToDictionary(
                    k => k.Key.Replace("InputLogin.", ""),
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return new JsonResult(new
            {
                success = false,
                errors
            });
        }


        // ===== LOGIN PROCESS =====
        var passwordHash = HashPassword(InputLogin.PasswordLogin);

        using var conn = Db.Connect();
        using var cmd = new SqlCommand(@"
        SELECT UserId, Nama, RoleType
        FROM PortalUsers
        WHERE Email = @Email
          AND PasswordHash = @PasswordHash
          AND IsActive = 1
    ", conn);

        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100)
            .Value = InputLogin.EmailLogin;

        cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255)
            .Value = passwordHash;

        conn.Open();
        using var rd = cmd.ExecuteReader();

        if (!rd.Read())
        {
            return new JsonResult(new
            {
                success = false,
                errors = new Dictionary<string, string[]>
            {
                { "EmailLogin", new[] { "Email atau password salah" } }
            }
            });
        }

        // // ===== SIMPAN SESSION =====
        // HttpContext.Session.SetInt32("UserId", rd.GetInt32(0));
        // HttpContext.Session.SetString("Nama", rd.GetString(1));
        // HttpContext.Session.SetString("RoleType", rd.GetString(2));

        // ------------- Set Cookie==========
        //  BUAT CLAIMS untuk isi daya
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, rd.GetInt32(0).ToString()),
        new Claim(ClaimTypes.Name, rd.GetString(1)),       // Nama
        new Claim(ClaimTypes.Role, rd.GetString(2))        // RoleType
    };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        // Untuk Cookie tetap di save walaupun browser di tutup
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,                 // cookie tetap ada
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // set expired bisa dengan detik, menit jam dan hari
        };

        // SIGN IN
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties
        );

        return new JsonResult(new
        {
            success = true,
            message = "Login berhasil"
        });
    }

    // =================END Login =================



    // ================= REGISTER =================
// Register dengan multi Upload Dokumen ======================================
public IActionResult OnPostRegister()
    {
        ModelState.Clear();
        using var conn = Db.Connect();
        conn.Open();

        // ================= VALIDASI PASSWORD =================
        if (Input.Password != Input.RetypePassword)
        {
            ModelState.AddModelError("Input.RetypePassword", "Password tidak sama");
        }

        // ================= VALIDASI EMAIL =================
        if (string.IsNullOrWhiteSpace(Input.Email))
        {
            ModelState.AddModelError("Input.Email", "Email wajib diisi");
        }
        else
        {
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Format email tidak valid");
            }
        }

        // ================= CEK EMAIL DUPLIKAT =================
        if (!string.IsNullOrWhiteSpace(Input.Email))
        {
            using var cmdCheckEmail = new SqlCommand(@"
                SELECT COUNT(1)
                FROM RegistrationRequests
                WHERE Email = @Email
            ", conn);


            cmdCheckEmail.Parameters.Add("@Email", SqlDbType.NVarChar, 150)
                .Value = Input.Email;

            if ((int)cmdCheckEmail.ExecuteScalar() > 0)
            {
                ModelState.AddModelError("Input.Email", "Email sudah pernah didaftarkan");
            }
        }

        // ================= VALIDASI MULTI FILE =================
        if (Input.OwnerDocuments == null || !Input.OwnerDocuments.Any())
        {
            ModelState.AddModelError(
                "Input.OwnerDocuments",
                "Minimal 1 dokumen harus diupload"
            );
        }
        else
        {
            var allowedExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

            foreach (var file in Input.OwnerDocuments)
            {
                var ext = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExt.Contains(ext))
                {
                    ModelState.AddModelError(
                        "Input.OwnerDocuments",
                        "Format file harus PDF / JPG / PNG"
                    );
                    break;
                }

                if (file.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "Input.OwnerDocuments",
                        "Ukuran tiap file maksimal 2MB"
                    );
                    break;
                }
            }
        }

        // ================= STOP JIKA ERROR =================
        if (!TryValidateModel(Input, nameof(Input)))
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Any())
                .ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return new JsonResult(new
            {
                success = false,
                errors
            });
        }

        // ================= SIMPAN FILE =================
        var savedFiles = new List<(string Path, string Name, string Type)>();

        var uploadsDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "documents"
        );

        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        foreach (var file in Input.OwnerDocuments)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = $"/uploads/documents/{fileName}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);

            savedFiles.Add((
                Path: filePath,
                Name: file.FileName,
                Type: file.ContentType
            ));
        }
        // ================= SIMPAN FILE END =================

        
        // ================= DATABASE TRANSACTION =================
        using var tran = conn.BeginTransaction();

        try
        {
            var passwordHash = HashPassword(Input.Password);
            int requestId; // 🔥 INT IDENTITY

            // ===== INSERT REGISTRATION + GET ID =====
            using var cmd = new SqlCommand(@"
                INSERT INTO RegistrationRequests
                (
                    RoleType, Email, PasswordHash,
                    FullName, KavlingDesc
                )
                VALUES
                (
                    @RoleType, @Email, @PasswordHash,
                    @FullName, @KavlingDesc
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", conn, tran);

            cmd.Parameters.Add("@RoleType", SqlDbType.VarChar, 20)
                .Value = Input.UserType;

            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150)
                .Value = Input.Email;

            cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255)
                .Value = passwordHash;

            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 150)
                .Value = string.IsNullOrWhiteSpace(Input.OwnerName)
                    ? DBNull.Value
                    : Input.OwnerName;


            cmd.Parameters.Add("@KavlingDesc", SqlDbType.NVarChar, 300)
                .Value = string.IsNullOrWhiteSpace(Input.KavlingDesc)
                    ? DBNull.Value
                    : Input.KavlingDesc;

            // 🔥 AMBIL RequestId (INT)
            requestId = (int)cmd.ExecuteScalar();

            // ===== INSERT DOCUMENTS =====
            foreach (var doc in savedFiles)
            {
                using var cmdDoc = new SqlCommand(@"
                    INSERT INTO RegistrationDocuments
                    (
                        RequestId, FileName, FilePath, FileType
                    )
                    VALUES
                    (
                        @RequestId, @FileName, @FilePath, @FileType
                    )
                ", conn, tran);

                cmdDoc.Parameters.AddWithValue("@RequestId", requestId);
                cmdDoc.Parameters.AddWithValue("@FileName", doc.Name);
                cmdDoc.Parameters.AddWithValue("@FilePath", doc.Path);
                cmdDoc.Parameters.AddWithValue("@FileType", doc.Type);

                cmdDoc.ExecuteNonQuery();
            }

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }

        //  Send email register ===========
            if (!string.IsNullOrWhiteSpace(Input.Email))
            {
                _email.Send(
                    Input.Email,
                    "Registrasi Berhasil – Menunggu Approval Admin",
                    $@"
                <div style='font-family: Arial, sans-serif; background:#f9fafb; padding:5px'>
                <div style='
                        max-width:600px;
                        margin:auto;
                        background:#ffffff;
                        border-radius:8px;
                        border:1px solid #e5e7eb;
                        overflow:hidden'>

                    <!-- HEADER -->
                    <div style='background:#16a34a; padding:16px; color:white'>
                    <h2 style='margin:0; font-size:18px'>
                        Registrasi Berhasil
                    </h2>
                    </div>

                    <!-- BODY -->
                    <div style='padding:20px; color:#374151'>

                    <p>Halo <b>{Input.OwnerName}</b>,</p>

                    <p>
                        Terima kasih telah melakukan registrasi di portal khusus penghuni kawasan kami.
                    </p>

                    <div style='
                        background:#ecfdf5;
                        border-left:4px solid #16a34a;
                        padding:12px;
                        margin:16px 0;
                        border-radius:4px;
                        color:#065f46'>
                        <b>Status Registrasi</b><br>
                        Registrasi Anda telah <b>berhasil dikirim</b> dan saat ini
                        <b>sedang diproses oleh admin kami</b>.
                    </div>

                    <p>
                        Anda akan menerima email lanjutan setelah akun Anda
                        disetujui dan diaktifkan.
                    </p>

                    <p style='margin-top:24px'>
                        Salam,<br>
                        <b>Pengelola Kawasan</b>
                    </p>

                    <hr style='margin:24px 0; border:none; border-top:1px solid #e5e7eb'>

                    <p style='font-size:12px; color:#6b7280'>
                        Email ini dikirim secara otomatis.<br>
                        Mohon tidak membalas email ini.
                    </p>

                    </div>
                </div>
                </div>
                "
                );
            }

            //  End Send email register ===========

        return new JsonResult(new
        {
            success = true,
            message = "Registrasi berhasil. Menunggu approval admin."
        });
    }

// Register dengan siggle Upload Dokumen ======================================
//     public IActionResult OnPostRegister()
//     {
//         ModelState.Clear();

//         using var conn = Db.Connect(); // Connection database
//         conn.Open();

//         if (Input.Password != Input.RetypePassword)
//         {
//             ModelState.AddModelError("Input.RetypePassword", "Password tidak sama");
//         }

//         // ===== VALIDASI EMAIL FORMAT =====
//         if (string.IsNullOrWhiteSpace(Input.Email))
//         {
//             ModelState.AddModelError("Input.Email", "Email wajib diisi");
//         }
//         else
//         {
//             var emailValidator = new EmailAddressAttribute();
//             if (!emailValidator.IsValid(Input.Email))
//             {
//                 ModelState.AddModelError("Input.Email", "Format email tidak valid");
//             }
//         }



//         // ===== CEK EMAIL SUDAH TERDAFTAR =====
//         if (!string.IsNullOrWhiteSpace(Input.Email))
//         {
//             using var cmdCheckEmail = new SqlCommand(@"
//                     SELECT COUNT(1)
//                     FROM RegistrationRequests
//                     WHERE Email = @Email
//                 ", conn);

//             cmdCheckEmail.Parameters.Add("@Email", SqlDbType.NVarChar, 100)
//             .Value = string.IsNullOrWhiteSpace(Input.Email)
//                 ? DBNull.Value
//                 : Input.Email;

//             // conn.Open();
//             int exists = (int)cmdCheckEmail.ExecuteScalar();

//             if (exists > 0)
//             {
//                 ModelState.AddModelError("Input.Email", "Email sudah pernah didaftarkan");
//             }
//         }

//         // ===== VALIDASI FILE =====
//         if (Input.OwnerDocument == null || Input.OwnerDocument.Length == 0)
//         {
//             ModelState.AddModelError("Input.OwnerDocument", "Dokumen wajib diupload");
//         }
//         else
//         {
//             var allowedExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
//             var ext = Path.GetExtension(Input.OwnerDocument.FileName).ToLower();

//             if (!allowedExt.Contains(ext))
//             {
//                 ModelState.AddModelError(
//                     "Input.OwnerDocument",
//                     "Format file harus PDF / JPG / PNG"
//                 );
//             }

//             // max 2MB
//             if (Input.OwnerDocument.Length > 2 * 1024 * 1024)
//             {
//                 ModelState.AddModelError(
//                     "Input.OwnerDocument",
//                     "Ukuran file maksimal 2MB"
//                 );
//             }
//         }
//         // ===== VALIDASI FILE  END=====





//         // keluar jika error
//         // 🔑 VALIDASI KHUSUS LOGIN SAJA            

//         if (!TryValidateModel(Input, nameof(Input)))
//         {
//             var errors = ModelState
//                 .Where(x => x.Value.Errors.Any())
//                 .ToDictionary(
//                     k => k.Key,
//                     v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
//                 );

//             return new JsonResult(new
//             {
//                 success = false,
//                 errors
//             });
//         }


//         // ===== SIMPAN FILE =====
//         string filePath = null;

//         if (Input.OwnerDocument != null)
//         {
//             var uploadsDir = Path.Combine(
//                 Directory.GetCurrentDirectory(),
//                 "wwwroot",
//                 "uploads",
//                 "documents"
//             );

//             if (!Directory.Exists(uploadsDir))
//                 Directory.CreateDirectory(uploadsDir);

//             var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Input.OwnerDocument.FileName)}";
//             filePath = $"/uploads/documents/{fileName}";

//             var fullPath = Path.Combine(uploadsDir, fileName);

//             using var stream = new FileStream(fullPath, FileMode.Create);
//             Input.OwnerDocument.CopyTo(stream);
//         }

//         // ===== SIMPAN FILE END=====




//         //Database Connect Insert data
//         var passwordHash = HashPassword(Input.Password);
//         using var cmd = new SqlCommand(@"
//             INSERT INTO RegistrationRequests
//             (
//                 RoleType, Email, PasswordHash,
//                 FullName,KavlingDesc, DocumentPath,FileName, FileType
//             )
//             VALUES
//             (
//                 @RoleType, @Email, @PasswordHash,
//                 @FullName,@KavlingDesc, @DocumentPath,@FileName, @FileType

//             )
//              ", conn);

//         // ================= ROLE =================
//         cmd.Parameters.Add("@RoleType", SqlDbType.VarChar, 20)
//             .Value = Input.UserType;

//         // ================= EMAIL =================
//         cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100)
//             .Value = string.IsNullOrWhiteSpace(Input.Email)
//                 ? DBNull.Value
//                 : Input.Email;

//         // ================= PASSWORD =================
//         cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255)
//             .Value = passwordHash;

//         // ================= FULL NAME (OWNER) =================
//         cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100)
//             .Value = string.IsNullOrWhiteSpace(Input.OwnerName)
//                 ? DBNull.Value
//                 : Input.OwnerName;


//         cmd.Parameters.Add("@KavlingDesc", SqlDbType.NVarChar, 300)
//             .Value = string.IsNullOrWhiteSpace(Input.KavlingDesc)
//                 ? DBNull.Value
//                 : Input.KavlingDesc;

//         cmd.Parameters.Add("@DocumentPath", SqlDbType.NVarChar, 255)
// .Value = (object?)filePath ?? DBNull.Value;

//         cmd.Parameters.AddWithValue("@FileName", Input.OwnerDocument.FileName);
//         cmd.Parameters.AddWithValue("@FileType", Input.OwnerDocument.ContentType);



//         // conn.Open();
//         cmd.ExecuteNonQuery(); // Execute query

//         //Send email register
//         if (!string.IsNullOrWhiteSpace(Input.Email))
//         {
//             _email.Send(
//                 Input.Email,
//                 "Registrasi Berhasil – Menunggu Approval Admin",
//                 $@"
//             <div style='font-family: Arial, sans-serif; background:#f9fafb; padding:5px'>
//             <div style='
//                     max-width:600px;
//                     margin:auto;
//                     background:#ffffff;
//                     border-radius:8px;
//                     border:1px solid #e5e7eb;
//                     overflow:hidden'>

//                 <!-- HEADER -->
//                 <div style='background:#16a34a; padding:16px; color:white'>
//                 <h2 style='margin:0; font-size:18px'>
//                     Registrasi Berhasil
//                 </h2>
//                 </div>

//                 <!-- BODY -->
//                 <div style='padding:20px; color:#374151'>

//                 <p>Halo <b>{Input.OwnerName}</b>,</p>

//                 <p>
//                     Terima kasih telah melakukan registrasi di portal khusus penghuni kawasan kami.
//                 </p>

//                 <div style='
//                     background:#ecfdf5;
//                     border-left:4px solid #16a34a;
//                     padding:12px;
//                     margin:16px 0;
//                     border-radius:4px;
//                     color:#065f46'>
//                     <b>Status Registrasi</b><br>
//                     Registrasi Anda telah <b>berhasil dikirim</b> dan saat ini
//                     <b>sedang diproses oleh admin kami</b>.
//                 </div>

//                 <p>
//                     Anda akan menerima email lanjutan setelah akun Anda
//                     disetujui dan diaktifkan.
//                 </p>

//                 <p style='margin-top:24px'>
//                     Salam,<br>
//                     <b>Pengelola Kawasan</b>
//                 </p>

//                 <hr style='margin:24px 0; border:none; border-top:1px solid #e5e7eb'>

//                 <p style='font-size:12px; color:#6b7280'>
//                     Email ini dikirim secara otomatis.<br>
//                     Mohon tidak membalas email ini.
//                 </p>

//                 </div>
//             </div>
//             </div>
//             "
//             );
//         }


//         return new JsonResult(new
//         {
//             success = true,
//             message = "Registrasi berhasil. Menunggu approval admin."
//         });

//         // TempData["Success"] = "Registrasi berhasil. Menunggu approval admin.";
//         // return RedirectToPage("/Login");

//     }



    // ================= INPUT MODEL =================

    public class LoginInputModel
    {
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string EmailLogin { get; set; }

        [Required(ErrorMessage = "Password wajib diisi")]
        public string PasswordLogin { get; set; }
    }

    public class InputModel
    {
        // ================= Register =================
        [Required]
        public string UserType { get; set; }

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password wajib diisi")]
        [MinLength(3, ErrorMessage = "Password minimal 3 karakter")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Ulangi password")]
        public string RetypePassword { get; set; }

        // public bool? HasKavling { get; set; }
        // public bool? HasKavlingCompany { get; set; }

        [Required(ErrorMessage = "Deskripsi harus di isi")]
        public string? KavlingDesc { get; set; }

        // 🔥 FILE UPLOAD
        [Required(ErrorMessage = "Dokumen pendukung wajib diupload")]
        // public IFormFile OwnerDocument { get; set; }
        public List<IFormFile> OwnerDocuments { get; set; } = new();
    }


    // ================= PASSWORD HASH =================
    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }




}

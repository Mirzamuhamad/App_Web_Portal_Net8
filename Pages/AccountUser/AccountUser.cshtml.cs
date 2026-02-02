using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper; // Sangat disarankan untuk mempermudah mapping data
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TestLandingPageNet8.Pages.AccountUser
{
    public class AccountUserModel : PageModel
    {

        public async Task<IActionResult> OnPostLogout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            // respon JSON untuk AJAX
            return new JsonResult(new { success = true, redirect = "/Login" });
        }

        public TenantViewModel Tenant { get; set; }
        public List<TicketViewModel> Complaints { get; set; }
        public List<InvoiceViewModel> Invoices { get; set; }
                        
        // Di dalam class AccountUserModel
        public List<ServiceOrderViewModel> ServiceOrders { get; set; } = new();


        //Get Data dari table PortalUsers ===================
        public async Task OnGetAsync()
        {
            // Data Dummy Profil
            // 1. Ambil UserId dari Claims (User yang sedang login)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int userId))
            {
                Response.Redirect("/Login");
                return;
            }

            if (string.IsNullOrEmpty(userIdStr))
            {
                Response.Redirect("/Login");
                return;
            }

            // 2. Gunakan function Db.Connect() Anda
            using (var connection = Db.Connect())
            {
                // Query mengambil data berdasarkan UserId
                string sql = "SELECT * FROM V_PortalUsers WHERE UserId = @Id";
                // Menggunakan Dapper untuk mapping otomatis ke class PortalUser
                Tenant = await connection.QueryFirstOrDefaultAsync<TenantViewModel>(sql, new { Id = userId });

                // 2. Query untuk List Pengaduan menggunakan View V_ComplaintList
                string sqlComplaints = "SELECT * FROM V_ComplaintList WHERE UserId = @Id ORDER BY ComplaintId DESC";
                Complaints = (await connection.QueryAsync<TicketViewModel>(sqlComplaints, new { Id = userId })).ToList();



// Di dalam OnGetAsync, tambahkan query:
var sqlService = "SELECT * FROM V_ServiceOrderList WHERE UserId = @Uid ORDER BY Date DESC";
ServiceOrders = (await connection.QueryAsync<ServiceOrderViewModel>(sqlService, new { Uid = userId })).ToList();
            }


            // Data Dummy Tagihan
            Invoices = new List<InvoiceViewModel>
            {
                new InvoiceViewModel { Period = "Januari 2026", Amount = 5500000, Status = "Belum Bayar" },
                new InvoiceViewModel { Period = "Desember 2025", Amount = 5500000, Status = "Lunas" },
                new InvoiceViewModel { Period = "November 2025", Amount = 5500000, Status = "Lunas" }
            };
        }
        //end get data portal users ===================


        //Start Update data Portal Users ==============
        // Handler khusus untuk Update via Fetch API (AJAX)
        public async Task<IActionResult> OnPostUpdateProfileAsync(IFormFile FotoFile)
        {
            try
            {
                // 1. Tangkap data dari form
                var userId = Request.Form["UserId"];
                var nama = Request.Form["Nama"];
                var noHp = Request.Form["NoHp"];
                var npwp = Request.Form["Npwp"];
                var jenisKelamin = Request.Form["JenisKelamin"];
                var alamatLengkap = Request.Form["AlamatLengkap"];
                var tempatLahir = Request.Form["TempatLahir"];
                var tanggalLahirStr = Request.Form["TanggalLahir"];

                // 2. Persiapkan parameter untuk Dapper
                var parameters = new DynamicParameters();
                parameters.Add("UserId", userId);
                parameters.Add("Nama", nama);
                parameters.Add("NoHp", noHp);
                parameters.Add("Npwp", npwp);
                parameters.Add("JenisKelamin", jenisKelamin);
                parameters.Add("AlamatLengkap", alamatLengkap);
                parameters.Add("TempatLahir", tempatLahir);
                parameters.Add("TanggalLahir", string.IsNullOrEmpty(tanggalLahirStr) ? null : DateTime.Parse(tanggalLahirStr));
                parameters.Add("UpdateBy", nama);

                string sqlUpdateFoto = "";
                string newFotoPath = "";

                // 3. Logika Upload Foto
                if (FotoFile != null && FotoFile.Length > 0)
                {
                    // Buat nama file unik
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(FotoFile.FileName);
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                    var filePath = Path.Combine(folderPath, fileName);

                    // Buat direktori jika belum ada
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await FotoFile.CopyToAsync(stream);
                    }

                    newFotoPath = "/uploads/profiles/" + fileName;

                    // Tambahkan kolom foto ke query update
                    sqlUpdateFoto = ", FileNameUsers = @FileName, DocumentPathUsers = @DocPath, FileTypeUsers = @FileType";
                    parameters.Add("FileName", fileName);
                    parameters.Add("DocPath", newFotoPath);
                    parameters.Add("FileType", FotoFile.ContentType);
                }

                // 4. Eksekusi Update ke Database
                using (var connection = Db.Connect())
                {
                    string query = $@"UPDATE PortalUsers 
                                     SET Nama = @Nama, 
                                         NoHp = @NoHp, 
                                         Npwp = @Npwp, 
                                         JenisKelamin = @JenisKelamin, 
                                         AlamatLengkap = @AlamatLengkap, 
                                         TempatLahir = @TempatLahir, 
                                         TanggalLahir = @TanggalLahir,
                                         UpdateBy = @UpdateBy,
                                         UpdateAt = SYSDATETIME()
                                         {sqlUpdateFoto}
                                     WHERE UserId = @UserId";

                    await connection.ExecuteAsync(query, parameters);
                }

                // 5. Kembalikan respon JSON sukses
                return new JsonResult(new
                {
                    success = true,
                    message = "Profil Anda berhasil diperbarui!",
                    newData = new
                    {
                        nama = nama,
                        fotoPath = newFotoPath // Akan kosong jika tidak ada upload baru
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }
        //End Update data Portal Users ==============

        //Start Change Password ==================
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<IActionResult> OnPostChangePasswordAsync(string oldPwd, string newPwd)
        {
            try
            {
                // 1. Ambil ID dari Claims (Cookie)
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdStr, out int userId))
                {
                    // Jika request via AJAX, sebaiknya return JSON agar Client bisa handle redirect
                    return new JsonResult(new { success = false, message = "Sesi berakhir, silakan login kembali.", redirect = "/Login" });
                }

                using (var connection = Db.Connect())
                {
                    // 2. Ambil hash password lama dari DB
                    var currentHash = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT PasswordHash FROM PortalUsers WHERE UserId = @UserId",
                        new { UserId = userId });

                    if (currentHash == null)
                        return new JsonResult(new { success = false, message = "User tidak ditemukan." });

                    // 3. Verifikasi Password Lama (SHA256)
                    if (currentHash != HashPassword(oldPwd))
                    {
                        return new JsonResult(new { success = false, message = "Password lama yang Anda masukkan salah!" });
                    }

                    // 4. Update Password Baru
                    string newHash = HashPassword(newPwd);
                    await connection.ExecuteAsync(
                        "UPDATE PortalUsers SET PasswordHash = @Hash, UpdateAt = SYSDATETIME() WHERE UserId = @UserId",
                        new { Hash = newHash, UserId = userId });

                    return new JsonResult(new { success = true, message = "Password berhasil diperbarui secara aman!" });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error: " + ex.Message });
            }
        }
        //End Change Password ==================


        public async Task<IActionResult> OnPostApprovePriceAsync(int serviceId, IFormFile poFile)
{
    if (poFile == null || poFile.Length == 0)
    {
        return new JsonResult(new { success = false, message = "File PO wajib diunggah." });
    }

    try
    {
        // 1. Proses Simpan File
        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/service_order/po_tenant");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string extension = Path.GetExtension(poFile.FileName);
        string uniqueFileName = $"PO_{serviceId}_{Guid.NewGuid().ToString()}{extension}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await poFile.CopyToAsync(stream);
        }

        // 2. Update Database (Menggunakan Dapper)
        using (var connection = Db.Connect())
        {
            // Update status dan kolom PoDokumen (Sesuaikan nama kolom di DB Anda)
            string sql = @"UPDATE ServiceOrderPortal 
                           SET Status = 'APPROVED', 
                               PoDokumen = @PoDokumen 
                           WHERE Id = @Id";

            int affectedRows = await connection.ExecuteAsync(sql, new { 
                PoDokumen = "/uploads/service_order/po_tenant/" + uniqueFileName, 
                Id = serviceId 
            });

            if (affectedRows > 0)
            {
                return new JsonResult(new { success = true, message = "Harga disetujui dan PO berhasil diunggah!" });
            }
        }

        return new JsonResult(new { success = false, message = "Gagal memperbarui data di database." });
    }
    catch (Exception ex)
    {
        return new JsonResult(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
    }
}


    }

    // ViewModel Sederhana
    // Class ini harus sesuai dengan struktur tabel PortalUsers Anda
    public class TenantViewModel
    {
        public int UserId { get; set; }
        public string Nama { get; set; }
        public string Email { get; set; }
        public string NoHp { get; set; }
        public string Npwp { get; set; }
        public string JenisKelamin { get; set; }
        public string TempatLahir { get; set; }
        public DateTime? TanggalLahir { get; set; }
        public string AlamatLengkap { get; set; }
        public string FileNameUsers { get; set; }
        public string DocumentPathUsers { get; set; }
        public string RoleType { get; set; }
        // Properti tambahan untuk tampilan unit (jika diperlukan)
        public string PropertyUnit { get; set; } 
    }
    public class TicketViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        // Field Tambahan
        public string Description { get; set; }
        public string ImageUrl { get; set; } // Berisi URL foto (atau string dipisah koma)
        public string KavlingName { get; set; }

        public int PhotoCount { get; set; }

        // Field baru untuk menghitung jumlah foto secara otomatis
        public int ImageCount => string.IsNullOrWhiteSpace(ImageUrl)
                                ? 0
                                : ImageUrl.Split(',').Length;
    }
    public class InvoiceViewModel { public string Period, Status; public decimal Amount; }

    public class TenantProfile
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PropertyUnit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string JoinDate { get; set; } = string.Empty;
    }

    // Tambahkan di dalam namespace yang sama
public class ServiceOrderViewModel
{
    public string ServiceId { get; set; }
    public string Id { get; set; } // TransNmbr
    public string TypeService { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime Date { get; set; }
    public string KavlingName { get; set; }
    public decimal Harga { get; set; }
    public decimal HargaSatuan { get; set; }
    public decimal Qty { get; set; }
    public string Item { get; set; }
    public string ImageUrl { get; set; }
    public string PoDokumen { get; set; }
    public int PhotoCount { get; set; }
}

}

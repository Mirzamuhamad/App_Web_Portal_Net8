using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Dapper; // Pastikan namespace Dapper sudah di-using

namespace TestLandingPageNet8.Pages.GeneralComplaint
{
    [AllowAnonymous] // Ubah/sesuaikan dengan kebutuhan autentikasi Anda
    public class GeneralComplaintModel : PageModel
    {
        [BindProperty]
        public GeneralComplaintInput Input { get; set; } = new GeneralComplaintInput();

        // Tambahan untuk injeksi EmailService
        private readonly IConfiguration _config;
        private readonly EmailService _email;

        public GeneralComplaintModel(IConfiguration config, EmailService email)
        {
            _config = config;
            _email = email;
        }

        public class GeneralComplaintInput
        {
            [Required]
            public string Nama { get; set; }

            // Tambahan untuk Substansi
            [Required]
            public string Substansi { get; set; }

            [Required]
            public string Phone { get; set; }

            [Required]
            public DateTime Schedule { get; set; }

            [Required]
            public string Description { get; set; }

            public List<IFormFile>? Photos { get; set; }
        }

        public void OnGet()
        {
            // Tidak perlu load KavlingList jika tidak digunakan
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "Lengkapi semua data laporan. .!" });
            }

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert ke Tabel GeneralComplaintPortal (Menambahkan kolom Substansi)
                        string sqlComplaint = @"INSERT INTO GeneralComplaintPortal (Nama, Substansi, Phone, Schedule, Description, Status) 
                                                OUTPUT INSERTED.Id 
                                                VALUES (@Nama, @Substansi, @Phone, @Schedule, @Description, 'Proses')";

                        int newComplaintId;
                        using (var cmd = new SqlCommand(sqlComplaint, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Nama", Input.Nama);
                            cmd.Parameters.AddWithValue("@Substansi", Input.Substansi);
                            cmd.Parameters.AddWithValue("@Phone", Input.Phone);
                            cmd.Parameters.AddWithValue("@Schedule", Input.Schedule);
                            cmd.Parameters.AddWithValue("@Description", Input.Description);
                            newComplaintId = (int)await cmd.ExecuteScalarAsync();
                        }

                        // 2. Upload Foto & Insert ke Tabel GeneralComplaintImages
                        if (Input.Photos != null && Input.Photos.Count > 0)
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/generalComplaint");
                            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                            foreach (var file in Input.Photos)
                            {
                                if (file.Length > 0)
                                {
                                    string ext = Path.GetExtension(file.FileName).ToLower();
                                    string[] allowed = { ".jpg", ".jpeg", ".png" };
                                    if (!allowed.Contains(ext)) continue;

                                    string uniqueName = Guid.NewGuid().ToString() + ext;
                                    string path = Path.Combine(uploadsFolder, uniqueName);

                                    using (var stream = new FileStream(path, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    string sqlImg = @"INSERT INTO GeneralComplaintImages (ComplaintId, FilePath, FileName, FileType) 
                                                     VALUES (@Cid, @Path, @Name, @Type)";

                                    using (var cmdImg = new SqlCommand(sqlImg, connection, transaction))
                                    {
                                        cmdImg.Parameters.AddWithValue("@Cid", newComplaintId);
                                        cmdImg.Parameters.AddWithValue("@Path", "/uploads/generalComplaint/" + uniqueName);
                                        cmdImg.Parameters.AddWithValue("@Name", file.FileName);
                                        cmdImg.Parameters.AddWithValue("@Type", file.ContentType);
                                        await cmdImg.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        // Format Nomor Tiket: GTC-YYMMDD-ID (Contoh: GTC-261016-11)
                        string dateStr = DateTime.Now.ToString("yyMMdd");
                        string ticketNo = $"GTC-{dateStr}-{newComplaintId}";

                        transaction.Commit();

                        // === 3. LOGIKA KIRIM EMAIL KE ADMIN DARI TABEL MsEmailAdmin ===
                        try
                        {
                            string sqlGetAdminEmails = "SELECT Email FROM MsEmailAdmin";
                            var adminEmails = await connection.QueryAsync<string>(sqlGetAdminEmails);

                            foreach (var adminEmail in adminEmails)
                            {
                                if (!string.IsNullOrWhiteSpace(adminEmail))
                                {
                                    _email.Send(
                                        adminEmail,
                                        $"[{ticketNo}] - Permintaan Counseling Baru dari {Input.Nama}",

                                        $@"
<div style='font-family: Arial, sans-serif; background:#f4f4f5; padding:20px; width:100%; box-sizing:border-box;'>
    <div style='width:100%; background:#ffffff; border-radius:12px; border:1px solid #e4e4e7; overflow:hidden;'>
        <div style='background:#0d9488; padding:20px; color:white'>
            <h2 style='margin:0; font-size:20px'>Permintaan Counseling Baru</h2>
            <p style='margin:5px 0 0 0; opacity:0.9'>No. Tiket: <b>{ticketNo}</b></p>
        </div>
        <div style='padding:20px; color:#3f3f46; line-height:1.6'>
            <p>Halo Admin,</p>
            <p>Terdapat pengajuan jadwal counseling baru dengan detail sebagai berikut:</p>
            
            <div style='background:#f0fdfa; border-radius:8px; padding:15px; margin:15px 0; border:1px solid #ccfbf1'>
                <table style='width:100%'>
                    <tr>
                        <td style='width:130px; color:#64748b'><b>No. Tiket</b></td>
                        <td>: <span style='color:#0d9488; font-weight:bold'>{ticketNo}</span></td>
                    </tr>
                    <tr>
                        <td style='color:#64748b'><b>Nama Lengkap</b></td>
                        <td>: {Input.Nama}</td>
                    </tr>
                    <tr>
                        <td style='color:#64748b'><b>Substansi</b></td>
                        <td>: {Input.Substansi}</td>
                    </tr>
                    <tr>
                        <td style='color:#64748b'><b>No. WhatsApp</b></td>
                        <td>: {Input.Phone}</td>
                    </tr>
                    <tr>
                        <td style='color:#64748b'><b>Jadwal Diskusi</b></td>
                        <td>: {Input.Schedule:dd MMM yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td style='vertical-align:top; color:#64748b'><b>Topik / Detail</b></td>
                        <td>: {Input.Description}</td>
                    </tr>
                </table>
            </div>
            
            <p>Silahkan login ke dashboard admin untuk memproses permintaan counseling ini.</p>
            <p style='font-size:12px; color:#a1a1aa; margin-top:30px; border-top:1px solid #eee; padding-top:10px'>
                Waktu Request : {DateTime.Now:dd MMM yyyy HH:mm}
            </p>
        </div>
    </div>
</div>"
                                    );
                                }
                            }
                        }
                        catch (Exception mailEx)
                        {
                            // Tampilkan error email agar terlihat di response JSON jika ingin di-debug
                            return new JsonResult(new { success = false, message = "Data berhasil disimpan, tapi Email Gagal: " + mailEx.Message });
                        }
                        return new JsonResult(new { success = true, message = "Counseling berhasil dikirim" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new JsonResult(new { success = false, message = "Database Error: " + ex.Message });
                    }
                }
            }
        }
    }
}
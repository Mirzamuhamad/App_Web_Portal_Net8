using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Dapper;   

namespace TestLandingPageNet8.Pages.CreateServiceOrder
{
    public class KavlingViewModel
    {
        public int Id { get; set; }
        public string KavlingCode { get; set; }
    }

    public class TypeServiceModel
    {
        public string TypeService { get; set; }
    }
    public class CreateServiceOrderModel : PageModel
    {
        // Properti untuk menampung list yang akan ditampilkan di Dropdown
        public List<KavlingViewModel> KavlingList { get; set; } = new List<KavlingViewModel>();

        public List<TypeServiceModel> TypeServiceList { get; set; } = new List<TypeServiceModel>();

        [BindProperty]
        public ComplaintInput Input { get; set; } = new ComplaintInput();

        //tmbahan untuk class email
        private readonly IConfiguration _config;
        private readonly EmailService _email;

        public CreateServiceOrderModel(IConfiguration config, EmailService email)
        {
            _config = config;
            _email = email;
        }
        // end of email

        public class ComplaintInput
        {
            [Required]
            public string KavlingId { get; set; }

            [Required]
            [StringLength(100)]
            public string Title { get; set; }

            [Required]
            public string Description { get; set; }

            public List<IFormFile>? Photos { get; set; }
        }

        // public void OnGet() { }
        public async Task OnGetAsync()
        {
            // Ambil data kavling dari database
            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();
                string sql = "SELECT kavlingid, kavlingCode FROM V_ListKavlingUserPOrtal WHERE UserId = @UserId";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", User.FindFirstValue(ClaimTypes.NameIdentifier));
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            KavlingList.Add(new KavlingViewModel
                            {
                                Id = reader.GetInt32(0),
                                KavlingCode = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            // =======================
            // 2️⃣ Ambil Type Service
            // =======================
            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();

                string sqlService = "SELECT * FROM V_GetTypeService ORDER BY Urutan ASC;";

                using (var cmdService = new SqlCommand(sqlService, connection))
                using (var reader = await cmdService.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        TypeServiceList.Add(new TypeServiceModel
                        {
                            TypeService = reader.GetString(0)
                        });
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "Lengkapi semua data yang diperlukan." });
            }

            // Ganti 'db.Connect()' dengan instance koneksi database Anda
            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Ambil KavlingCode untuk keperluan email (MENGATASI ERROR CS0103)
                string sqlKavling = "SELECT KavlingCode FROM V_ListKavlingUserPOrtal WHERE KavlingId = @KavlingId";
                string selectedKavlingCode = "";
                using (var cmdKav = new SqlCommand(sqlKavling, connection, transaction))
                {
                    cmdKav.Parameters.AddWithValue("@KavlingId", Input.KavlingId);
                    var result = await cmdKav.ExecuteScalarAsync();
                    selectedKavlingCode = result?.ToString() ?? "-";
                }
                        // 1. Insert ke Tabel Utama
                        string sqlComplaint = @"INSERT INTO ServiceOrderPortal (KavlingId, TypeService, Description, CreatedAt, CreatedBy) 
                                                OUTPUT INSERTED.Id 
                                                VALUES (@KavlingId, @Title, @Description, GETDATE(), @UserId)";

                        int newComplaintId;
                        using (var cmd = new SqlCommand(sqlComplaint, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@KavlingId", Input.KavlingId);
                            cmd.Parameters.AddWithValue("@Title", Input.Title);
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@Description", Input.Description);
                            newComplaintId = (int)await cmd.ExecuteScalarAsync();
                        }

                        // 2. Upload Foto & Insert ke Tabel Detail
                        if (Input.Photos != null && Input.Photos.Count > 0)
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/service_order");
                            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                            foreach (var file in Input.Photos)
                            {
                                if (file.Length > 0)
                                {



                                    string ext = Path.GetExtension(file.FileName);
                                    string[] allowed = { ".jpg", ".jpeg", ".png" };
                                    if (!allowed.Contains(ext)) continue;
                                    string uniqueName = Guid.NewGuid().ToString() + ext;
                                    string path = Path.Combine(uploadsFolder, uniqueName);

                                    using (var stream = new FileStream(path, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    string sqlImg = @"INSERT INTO ServiceOrderImages (ServiceId, FilePath, FileName, FileType) 
                                                     VALUES (@Cid, @Path, @Name, @Type)";

                                    using (var cmdImg = new SqlCommand(sqlImg, connection, transaction))
                                    {
                                        cmdImg.Parameters.AddWithValue("@Cid", newComplaintId);
                                        cmdImg.Parameters.AddWithValue("@Path", "/uploads/service_order/" + uniqueName);
                                        cmdImg.Parameters.AddWithValue("@Name", file.FileName);
                                        cmdImg.Parameters.AddWithValue("@Type", file.ContentType);
                                        await cmdImg.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        // Ambil Nomor Referensi (TransNmbr) yang digenerate sistem
                        string transNmbr = "";
                        string sqlGetTrans = "SELECT TransNmbr FROM ServiceOrderPortal WHERE Id = @Id";
                        using (var cmdTrans = new SqlCommand(sqlGetTrans, connection, transaction))
                        {
                            cmdTrans.Parameters.AddWithValue("@Id", newComplaintId);
                            var resTrans = await cmdTrans.ExecuteScalarAsync();
                            transNmbr = resTrans?.ToString() ?? "";
                        }

                        transaction.Commit();
                     // === LOGIKA KIRIM EMAIL KE ADMIN dari table master email===
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
                                    $"[{transNmbr}] - {Input.Title} - {selectedKavlingCode}", // Tambahkan transNmbr di Subjek
                                    $@"
                                    <div style='font-family: Arial, sans-serif; background:#f4f4f5; padding:20px'>
                                        <div style='max-width:600px; margin:auto; background:#ffffff; border-radius:12px; border:1px solid #e4e4e7; overflow:hidden;'>
                                            <div style='background:#0d9488; padding:20px; color:white'>
                                                <h2 style='margin:0; font-size:20px'>Permintaan Service Request Baru</h2>
                                                <p style='margin:5px 0 0 0; opacity:0.9'>No. Referensi: <b>{transNmbr}</b></p>
                                            </div>
                                            <div style='padding:10px; color:#3f3f46; line-height:1.6'>
                                                <p>Halo Admin,</p>
                                                <p>Terdapat permintaan layanan baru dengan detail sebagai berikut:</p>
                                                
                                                <div style='background:#f0fdfa; border-radius:8px; padding:6px; margin:10px 0; border:1px solid #ccfbf1'>
                                                    <table style='width:100%'>
                                                        <tr>
                                                            <td style='width:130px; color:#64748b'><b>No. Tiket</b></td>
                                                            <td>: <span style='color:#0d9488; font-weight:bold'>{transNmbr}</span></td>
                                                        </tr>
                                                        <tr>
                                                            <td style='color:#64748b'><b>Unit/Kavling</b></td>
                                                            <td>: {selectedKavlingCode}</td>
                                                        </tr>
                                                        <tr>
                                                            <td style='color:#64748b'><b>Jenis Layanan</b></td>
                                                            <td>: {Input.Title}</td>
                                                        </tr>
                                                        <tr>
                                                            <td style='vertical-align:top; color:#64748b'><b>Deskripsi</b></td>
                                                            <td>: {Input.Description}</td>
                                                        </tr>
                                                    </table>
                                                </div>
                                                
                                                <p>Silahkan login ke dashboard kawasan untuk memproses permintaan ini.</p>
                                                <p style='font-size:12px; color:#a1a1aa; margin-top:30px; border-top:1px solid #eee; padding-top:10px'>
                                                    Waktu Request   : {DateTime.Now:dd MMM yyyy HH:mm}
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
                    Console.WriteLine("Email Error: " + mailEx.Message);
                }

                return new JsonResult(new { success = true, message = "Service Order Request berhasil dikirim!" });
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
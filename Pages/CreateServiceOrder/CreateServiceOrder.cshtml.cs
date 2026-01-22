using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.CreateServiceOrder
{
    public class KavlingViewModel
    {
        public int Id { get; set; }
        public string KavlingCode { get; set; }
    }
    public class CreateServiceOrderModel : PageModel
    {
        // Properti untuk menampung list yang akan ditampilkan di Dropdown
        public List<KavlingViewModel> KavlingList { get; set; } = new List<KavlingViewModel>();

        [BindProperty]
        public ComplaintInput Input { get; set; } = new ComplaintInput();

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
                        // 1. Insert ke Tabel Utama
                        string sqlComplaint = @"INSERT INTO ComplaintPortal (KavlingId, Title, Description, Status, CreatedAt, CreatedBy) 
                                                OUTPUT INSERTED.Id 
                                                VALUES (@KavlingId, @Title, @Description, 'Proses', GETDATE(), @UserId)";

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
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/complaints");
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

                                    string sqlImg = @"INSERT INTO ComplaintImages (ComplaintId, FilePath, FileName, FileType) 
                                                     VALUES (@Cid, @Path, @Name, @Type)";

                                    using (var cmdImg = new SqlCommand(sqlImg, connection, transaction))
                                    {
                                        cmdImg.Parameters.AddWithValue("@Cid", newComplaintId);
                                        cmdImg.Parameters.AddWithValue("@Path", "/uploads/complaints/" + uniqueName);
                                        cmdImg.Parameters.AddWithValue("@Name", file.FileName);
                                        cmdImg.Parameters.AddWithValue("@Type", file.ContentType);
                                        await cmdImg.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return new JsonResult(new { success = true, message = "Laporan Pengaduan berhasil dikirim!" });
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
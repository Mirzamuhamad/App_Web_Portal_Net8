using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; // Tambahkan ini

namespace TestLandingPageNet8.Pages.GeneralComplaint
{
    [AllowAnonymous] // Tambahkan ini agar bisa diakses tanpa login
    public class GeneralComplaintModel : PageModel
    {
        [BindProperty]
        public GeneralComplaintInput Input { get; set; } = new GeneralComplaintInput();

        public class GeneralComplaintInput
        {
            [Required]
            public string Nama { get; set; }

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
            // Tidak perlu lagi load KavlingList
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
                        // 1. Insert ke Tabel GeneralComplaintPortal
                        string sqlComplaint = @"INSERT INTO GeneralComplaintPortal (Nama, Phone, Schedule, Description, Status) 
                                                OUTPUT INSERTED.Id 
                                                VALUES (@Nama, @Phone, @Schedule, @Description, 'Proses')";

                        int newComplaintId;
                        using (var cmd = new SqlCommand(sqlComplaint, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Nama", Input.Nama);
                            cmd.Parameters.AddWithValue("@Phone", Input.Phone);
                            cmd.Parameters.AddWithValue("@Schedule", Input.Schedule);
                            cmd.Parameters.AddWithValue("@Description", Input.Description);                            
                            newComplaintId = (int)await cmd.ExecuteScalarAsync();
                        }

                        // 2. Upload Foto & Insert ke Tabel ComplaintImages
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

                        transaction.Commit();
                        return new JsonResult(new { success = true, message = "Laporan Pengaduan berhasil dikirim, Terimakasih atas laporan anda" });
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
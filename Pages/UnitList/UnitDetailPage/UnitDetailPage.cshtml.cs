using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;


namespace TestLandingPageNet8.Pages.UnitList.UnitDetailPage
{
    public class UnitDetailPageModel : PageModel
    {
        // Properti untuk menampung data yang akan ditampilkan di UI
        public KavlingInfo Unit { get; set; } = new KavlingInfo();
        public List<TicketViewModel> Complaints { get; set; } = new List<TicketViewModel>();

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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            using (var connection = Db.Connect())
            {

                // 1. Ambil UserId dari Claims (User yang sedang login)
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToPage("/Login");
                }

                if (string.IsNullOrEmpty(userIdStr))
                {
                    return RedirectToPage("/Login");
                }

                await connection.OpenAsync();

                // Pastikan variabel query diakhiri dengan titik koma (;)
                string sqlUnit = "SELECT KavlingId, KavlingCode, Kawasan FROM V_ListKavlingUserPOrtal WHERE KavlingId = @KavlingId AND UserId = @UserId;";
                Unit = await connection.QueryFirstOrDefaultAsync<KavlingInfo>(sqlUnit, new { KavlingId = id, UserId = userId });

                if (Unit == null)
                {
                    return RedirectToPage("/Index");
                }

                // Baris ini sering menjadi penyebab error jika mapping-nya tidak pas
                string sqlComplaints = "SELECT * FROM V_ComplaintList WHERE KavlingId = @KavlingId and UserId = @UserId ORDER BY date DESC";
                var result = await connection.QueryAsync<TicketViewModel>(sqlComplaints, new { KavlingId = id, UserId = userId });
                Complaints = result.ToList();
            }
            return Page();
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

        
    

        // Deklarasikan class pembantu DI LUAR method OnGetAsync
        public class KavlingInfo
        {
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; }
            public string Kawasan { get; set; }
        }

        public class TicketViewModel
        {
            public string Id { get; set; }
            public string TicketNumber { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; }
            public string ImageUrl { get; set; }
            public int PhotoCount { get; set; }
        }

        
    }
}
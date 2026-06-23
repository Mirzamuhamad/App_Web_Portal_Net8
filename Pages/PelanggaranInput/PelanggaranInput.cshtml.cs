using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Dapper;

namespace TestLandingPageNet8.Pages.Pelanggaran
{
    // [Authorize(Roles = "Owner,Security")] // Hanya mengizinkan Owner atau Security
    [AllowAnonymous] 
    public class InputModel : PageModel
    {
        [BindProperty]
        public ViolationFormInput Input { get; set; } = new();
        public List<KavlingDropdownDto> KavlingList { get; set; } = new();

        public class ViolationFormInput
        {
            // Diubah menjadi nullable int? agar dapat menerima nilai null
            public int? KavlingId { get; set; }

            [Required(ErrorMessage = "Jenis pelanggaran wajib diisi")]
            public string ViolationType { get; set; } = string.Empty;

            // Diubah menjadi nullable decimal? agar dapat menerima nilai null
            public decimal? FineAmount { get; set; }

            [Required(ErrorMessage = "Kronologi wajib diisi")]
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Waktu kejadian harus ditentukan")]
            public DateTime ViolationDate { get; set; }

            public List<IFormFile>? Photos { get; set; }
        }

        public class KavlingDropdownDto
        {
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            using (var connection = Db.Connect())
            {
                string sql = "SELECT KavlingId, KavlingCode FROM MsKavlingsPortal ORDER BY KavlingCode ASC";
                KavlingList = (await connection.QueryAsync<KavlingDropdownDto>(sql)).ToList();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "Lengkapi semua data isian formulir pelanggaran!" });
            }

            var createdByStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(createdByStr, out int currentUserId);

            string transNmbr = $"VIO/{DateTime.Now:yyyyMM}/{new Random().Next(1000, 9999)}";
            
            // Logika penentuan status awal
            string initialStatus = (Input.FineAmount.HasValue && Input.FineAmount.Value > 0) ? "Belum Lunas" : "Peringatan";

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sqlViolation = @"INSERT INTO KawasanViolation 
                                                (ViolationTransNmbr, KavlingId, ViolationType, Description, ViolationDate, FineAmount, ViolationStatus, CreatedBy, CreatedDate) 
                                                OUTPUT INSERTED.ViolationId 
                                                VALUES (@TransNo, @KavlingId, @Type, @Desc, @VioDate, @Fine, @Status, @CreatedBy, GETDATE())";

                        int newViolationId;
                        using (var cmd = new SqlCommand(sqlViolation, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TransNo", transNmbr);
                            
                            // Handling nilai NULL untuk KavlingId
                            cmd.Parameters.AddWithValue("@KavlingId", (object)Input.KavlingId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Type", System.Net.WebUtility.HtmlEncode(Input.ViolationType));
                            cmd.Parameters.AddWithValue("@Desc", System.Net.WebUtility.HtmlEncode(Input.Description));
                            cmd.Parameters.AddWithValue("@VioDate", Input.ViolationDate);
                            
                            // Handling nilai NULL untuk FineAmount
                            cmd.Parameters.AddWithValue("@Fine", (object)Input.FineAmount ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", initialStatus);
                            cmd.Parameters.AddWithValue("@CreatedBy", currentUserId);
                            
                            newViolationId = (int)await cmd.ExecuteScalarAsync();
                        }

                        if (Input.Photos != null && Input.Photos.Count > 0)
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/pelanggaran");
                            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                            foreach (var file in Input.Photos)
                            {
                                if (file.Length > 0)
                                {
                                    string ext = Path.GetExtension(file.FileName).ToLower();
                                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                                    if (!allowedExtensions.Contains(ext)) continue;

                                    string uniqueFileName = Guid.NewGuid().ToString() + ext;
                                    string fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                                    using (var stream = new FileStream(fullPath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    string sqlImg = @"INSERT INTO KawasanViolationEvidence (ViolationId, ImagePath) 
                                                     VALUES (@Vid, @Path)";

                                    using (var cmdImg = new SqlCommand(sqlImg, connection, transaction))
                                    {
                                        cmdImg.Parameters.AddWithValue("@Vid", newViolationId);
                                        cmdImg.Parameters.AddWithValue("@Path", "/uploads/pelanggaran/" + uniqueFileName);
                                        await cmdImg.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return new JsonResult(new { success = true, message = $"Laporan {transNmbr} berhasil disimpan!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new JsonResult(new { success = false, message = "Sistem Error Database: " + ex.Message });
                    }
                }
            }
        }
    }
}
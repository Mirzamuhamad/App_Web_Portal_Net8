using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace TestLandingPageNet8.Pages.PermitPage
{
    public class PermitPageModel : PageModel
    {
        public List<KavlingInfo> MyUnits { get; set; } = new();
        public List<PermitViewModel> PermitHistory { get; set; } = new(); // Frontend di atas sekarang memanggil properti ini

        [BindProperty]
        public PermitInput Input { get; set; } = new PermitInput();

        public class PermitInput
        {
            [Required(ErrorMessage = "Unit / Kavling harus dipilih")]
            public int KavlingId { get; set; }

            [Required(ErrorMessage = "Jenis izin harus diisi")]
            public string PermitType { get; set; }

            [Required(ErrorMessage = "Detail keperluan izin harus diisi")]
            public string Description { get; set; }

            public DateTime? ProposedDate { get; set; }

            public List<IFormFile>? Attachments { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            using (var connection = Db.Connect())
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToPage("/Login");
                }

                await connection.OpenAsync();

                string sqlUnits = "SELECT KavlingId, KavlingCode, Kawasan FROM V_ListKavlingUserPOrtal WHERE UserId = @UserId;";
                var unitsResult = await connection.QueryAsync<KavlingInfo>(sqlUnits, new { UserId = userId });
                MyUnits = unitsResult.ToList();

                string sqlPermits = @"SELECT p.*, k.KavlingCode 
                                      FROM V_GetTenantPermits p
                                      INNER JOIN MsKavlingsPortal k ON p.KavlingId = k.KavlingId
                                      WHERE p.UserId = @UserId 
                                      ORDER BY p.CreatedDate DESC";
                var permitsResult = await connection.QueryAsync<PermitViewModel>(sqlPermits, new { UserId = userId });
                PermitHistory = permitsResult.ToList();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Sesi Anda telah habis." });
            }

            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "Lengkapi semua data yang diperlukan." });
            }

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();

                string sqlCheck = "SELECT COUNT(1) FROM V_ListKavlingUserPOrtal WHERE KavlingId = @KavlingId AND UserId = @UserId";
                int isOwner = await connection.ExecuteScalarAsync<int>(sqlCheck, new { KavlingId = Input.KavlingId, UserId = userId });
                if (isOwner == 0)
                {
                    return new JsonResult(new { success = false, message = "Unit / Kavling tidak valid." });
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string transNo = $"PRM/{DateTime.Now:yyyyMM}/{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                        DateTime finalProposedDate = Input.ProposedDate ?? DateTime.Now;

                        string sqlInsertPermit = @"INSERT INTO KawasanPermit (PermitTransNmbr, KavlingId, UserId, PermitType, Description, ProposedDate, PermitStatus, CreatedBy, CreatedDate) 
                                                   OUTPUT INSERTED.PermitId 
                                                   VALUES (@PermitTransNmbr, @KavlingId, @UserId, @PermitType, @Description, @ProposedDate, 'Pending', @UserId, GETDATE())";

                        int newPermitId;
                        using (var cmd = new SqlCommand(sqlInsertPermit, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@PermitTransNmbr", transNo);
                            cmd.Parameters.AddWithValue("@KavlingId", Input.KavlingId);
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@PermitType", Input.PermitType);
                            cmd.Parameters.AddWithValue("@Description", Input.Description);
                            cmd.Parameters.AddWithValue("@ProposedDate", finalProposedDate);
                            newPermitId = (int)await cmd.ExecuteScalarAsync();
                        }

                        if (Input.Attachments != null && Input.Attachments.Count > 0)
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/permits");
                            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                            foreach (var file in Input.Attachments)
                            {
                                if (file.Length > 0)
                                {
                                    string ext = Path.GetExtension(file.FileName).ToLower();
                                    string[] allowed = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                                    if (!allowed.Contains(ext)) continue;

                                    string uniqueName = Guid.NewGuid().ToString() + ext;
                                    string path = Path.Combine(uploadsFolder, uniqueName);

                                    using (var stream = new FileStream(path, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    string sqlImg = @"INSERT INTO KawasanPermitAttachment (PermitId, DocumentName, FilePath, UploadedDate) 
                                                     VALUES (@PermitId, @DocumentName, @FilePath, GETDATE())";

                                    using (var cmdFile = new SqlCommand(sqlImg, connection, transaction))
                                    {
                                        cmdFile.Parameters.AddWithValue("@PermitId", newPermitId);
                                        cmdFile.Parameters.AddWithValue("@DocumentName", file.FileName);
                                        cmdFile.Parameters.AddWithValue("@FilePath", "/uploads/permits/" + uniqueName);
                                        await cmdFile.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return new JsonResult(new { success = true, message = "Permohonan Izin berhasil dikirim!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new JsonResult(new { success = false, message = "Database Error: " + ex.Message });
                    }
                }
            }
        }

        public class KavlingInfo
        {
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; }
            public string Kawasan { get; set; }
        }

        public class PermitViewModel
        {
            public int PermitId { get; set; }
            public string PermitTransNmbr { get; set; }
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; }
            public int UserId { get; set; }
            public string PermitType { get; set; }
            public string Description { get; set; }
            public DateTime ProposedDate { get; set; }
            public string PermitStatus { get; set; }
            public string NotesFromManagement { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
            public string ThumbnailPath { get; set; }
            public string DocumentUrls { get; set; }
            public int DocumentCount { get; set; }
        }
    }
}
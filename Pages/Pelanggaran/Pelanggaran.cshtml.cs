using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using System.Security.Claims;
using System.Data;

namespace TestLandingPageNet8.Pages.Pelanggaran
{
    public class ListModel : PageModel
    {
        public List<ViolationListDto> ViolationItems { get; set; } = new();
        public decimal TotalDendaBelumLunas { get; set; }

        public async Task<IActionResult> OnGetAsync(int kavlingId)
        {
            // 1. Ambil UserId dari Claims (User yang sedang login)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Login");
            }

            #region ================= MODE 1: REAL DATABASE (DAPPER) =================
            
            try
            {
                using (var connection = Db.Connect())
                {
                    // Query mengambil data pelanggaran berdasarkan KavlingId dan UserId penyewa sesuai kolom View terbaru
                    string sql = @"SELECT * FROM V_GetTenantViolations 
                                //    WHERE KavlingId = @KavlingId AND UserId = @UserId
                                   ORDER BY ViolationDate DESC";

                    ViolationItems = (await connection.QueryAsync<ViolationListDto>(sql, new { 
                        KavlingId = kavlingId, 
                        UserId = userId 
                    })).ToList();
                }
            }
            catch (Exception)
            {
                ViolationItems = new List<ViolationListDto>();
            }
            

            #endregion ===============================================================

            /*#region ================= MODE 2: DATA DUMMY TESTING =====================
            // Data dummy disesuaikan persis dengan skema output string koma (STRING_AGG) dan PhotoCount dari SQL View
            ViolationItems = new List<ViolationListDto>
            {
                new() { 
                    ViolationId = 1, 
                    ViolationTransNmbr = "VIO/202606/0001", 
                    KavlingId = kavlingId, 
                    ViolationType = "Parkir Sembarangan", 
                    Description = "Mobil Nissan Grand Livina menghalangi jalan utama ring 1 dekat kavling tetangga.", 
                    ViolationDate = DateTime.Now.AddDays(-2), 
                    FineAmount = 150000, 
                    ViolationStatus = "Peringatan", 
                    ThumbnailPath = "/Image/Image9.jpg",
                    ImageUrl = "/Image/Image9.jpg,/Image/Image11.jpg",
                    PhotoCount = 10
                },
                new() { 
                    ViolationId = 2, 
                    ViolationTransNmbr = "VIO/202606/0002", 
                    KavlingId = kavlingId, 
                    ViolationType = "Renovasi Tanpa Izin", 
                    Description = "Melakukan pembongkaran pagar depan tanpa menyerahkan berkas izin resmi ke admin kawasan.", 
                    ViolationDate = DateTime.Now.AddDays(-5), 
                    FineAmount = 0, 
                    ViolationStatus = "Peringatan", 
                    ThumbnailPath = "/Image/Image10.jpg", 
                    ImageUrl = "/Image/Image10.jpg",
                    PhotoCount = 6
                }
            };
            #endregion ===============================================================
            */

            // Hitung otomatis total denda yang belum dibayar
            TotalDendaBelumLunas = ViolationItems
                .Where(x => x.ViolationStatus == "Belum Lunas")
                .Sum(x => x.FineAmount);

            return Page();
        }
    }

    public class ViolationListDto
    {
        public int ViolationId { get; set; }
        public string ViolationTransNmbr { get; set; } = string.Empty;
        public int KavlingId { get; set; }
        public int UserId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ViolationDate { get; set; }
        public decimal FineAmount { get; set; }
        public string ViolationStatus { get; set; } = string.Empty;
        
        // --- Tambahan field baru menyesuaikan SQL VIEW Anda ---
        public string ThumbnailPath { get; set; } = "/uploads/evidences/default.png";
        public string ImageUrl { get; set; } = string.Empty;
        public int PhotoCount { get; set; }
    }
}
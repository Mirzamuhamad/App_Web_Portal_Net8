using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;


namespace TestLandingPageNet8.Pages.TagihanUnitList.TagihanUnitDetailPage
{
    public class TagihanUnitDetailPageModel : PageModel
    {
        // Properti untuk menampung data yang akan ditampilkan di UI
        public KavlingInfo Unit { get; set; } = new KavlingInfo();

        public List<KavlingInfoDetail> UnitDetailTagihan { get; set; } = new();
        public decimal TotalSemuaTagihan => UnitDetailTagihan?.Sum(x => x.AmountPerKavling) ?? 0;
        public DateTime? DueDateUtama => UnitDetailTagihan?.FirstOrDefault()?.DueDate;
        public string StatusUtama => UnitDetailTagihan?.FirstOrDefault()?.Status ?? "Kosong";
        public string CustomerCode { get; set; } = string.Empty;

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

        public async Task<IActionResult> OnGetAsync(string? invoiceNo)
        {
            invoiceNo = string.IsNullOrWhiteSpace(invoiceNo) ? null : invoiceNo.Trim();

            using (var connection = Db.Connect())
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToPage("/Login");
                }

                await connection.OpenAsync();

                const string customerSql = @"
                    SELECT NULLIF(LTRIM(RTRIM(CustCode)), '')
                    FROM V_PortalUsers
                    WHERE UserId = @UserId;";

                var customerCode = await connection.QueryFirstOrDefaultAsync<string?>(
                    customerSql,
                    new { UserId = userId });
                CustomerCode = customerCode ?? string.Empty;

                const string invoiceSql = @"
                    SELECT DISTINCT
                        T.TransNmbr,
                        T.CustCode,
                        T.DueDate,
                        T.KavlingId,
                        K.KavlingCode,
                        K.Luas,
                        T.CommercialItem,
                        T.CommercialDesc,
                        T.AmountPerKavling,
                        T.TotalAmountKavling,
                        T.Status
                    FROM V_GetTagihanDetailKavling T
                    LEFT JOIN MsKavlingsPortal K ON K.KavlingId = T.KavlingId
                    WHERE (
                            T.UserId = @UserId
                            OR (@CustCode IS NOT NULL AND T.CustCode = @CustCode)
                          )
                      AND (@InvoiceNo IS NULL OR T.TransNmbr = @InvoiceNo)
                    ORDER BY T.DueDate, T.TransNmbr, T.KavlingId, T.CommercialItem;";

                UnitDetailTagihan = (await connection.QueryAsync<KavlingInfoDetail>(
                    invoiceSql,
                    new
                    {
                        InvoiceNo = invoiceNo,
                        UserId = userId,
                        CustCode = customerCode
                    })).ToList();

                if (UnitDetailTagihan.Count == 0)
                {
                    return Page();
                }

                var firstItem = UnitDetailTagihan[0];
                CustomerCode = firstItem.CustCode;

                var distinctUnits = UnitDetailTagihan
                    .GroupBy(x => new { x.KavlingId, x.KavlingCode })
                    .Select(x => x.First())
                    .ToList();

                Unit = new KavlingInfo
                {
                    KavlingId = firstItem.KavlingId,
                    KavlingCode = string.Join(", ", distinctUnits.Select(x => x.KavlingCode)),
                    Kawasan = "Area Kawasan",
                    Luas = distinctUnits.Sum(x => x.Luas)
                };

                const string complaintsSql = @"
                    SELECT *
                    FROM V_ComplaintList
                    WHERE UserId = @UserId
                      AND KavlingId IN @KavlingIds
                    ORDER BY date DESC;";

                var kavlingIds = distinctUnits.Select(x => x.KavlingId).ToArray();
                var result = await connection.QueryAsync<TicketViewModel>(
                    complaintsSql,
                    new { UserId = userId, KavlingIds = kavlingIds });
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

             public decimal Luas { get; set; }
        }

        public class KavlingInfoDetail
        {
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; } = string.Empty;
            public decimal Luas { get; set; }
            public string TransNmbr { get; set; } = string.Empty;
            public string CustCode { get; set; } = string.Empty;
            public DateTime? DueDate { get; set; }
            public string CommercialItem { get; set; } = string.Empty;
            public string CommercialDesc { get; set; } = string.Empty;
            public decimal AmountPerKavling { get; set; }
            public decimal TotalAmountKavling { get; set; }
           
            public string Status { get; set; } = string.Empty;
        }


        public class TicketViewModel
        {
            public string Id { get; set; }
            public string TicketNumber { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime Date { get; set; }
            public string Status { get; set; }
            public string ImageUrl { get; set; }
            public int PhotoCount { get; set; }
        }


    }
}

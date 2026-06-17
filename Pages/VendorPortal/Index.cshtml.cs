using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Security.Claims;
using System.Data;

namespace TestLandingPageNet8.Pages.VendorPortal
{
    public class IndexModel : PageModel
    {
        public string VendorName { get; set; } = string.Empty;
        public string VendorCode { get; set; } = string.Empty;
        public bool IsDemoMode { get; set; } = false;
        public List<RpoHeaderViewModel> RpoList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdStr) || role != "VENDOR")
            {
                return RedirectToPage("/Login");
            }

            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Login");
            }

            using (var conn = Db.Connect())
            {
                // 1. Ambil info vendor dari PortalUsers
                var user = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Nama, SuppCode FROM PortalUsers WHERE UserId = @UserId",
                    new { UserId = userId }
                );

                if (user != null)
                {
                    VendorName = user.Nama ?? string.Empty;
                    VendorCode = user.SuppCode ?? string.Empty;
                }

                // 2. Ambil data RPO (Dibatasi TOP 10 agar render JSON di html tidak crash karena kepenuhan)
                var headers = (await conn.QueryAsync<RpoHeaderViewModel>(@"SELECT TransNmbr, Status, TransDate, PONo, SuppCode, Remark, TotalForex, UserPrep, DatePrep
                    FROM STCRRPOHd WHERE SuppCode = @SuppCode
                    ORDER BY TransDate DESC, TransNmbr DESC",
                    new { SuppCode = VendorCode }
                )).ToList();

                // 3. Fallback ke Mode Demo jika kosong
                if (headers.Count == 0)
                {
                    IsDemoMode = true;
                    headers = (await conn.QueryAsync<RpoHeaderViewModel>(@"
                        SELECT TOP 10 TransNmbr, Status, TransDate, PONo, SuppCode, Remark, TotalForex, UserPrep, DatePrep
                        FROM STCRRPOHd
                        ORDER BY TransDate DESC, TransNmbr DESC"
                    )).ToList();
                }

                // Bersihkan teks dari karakter enter/pindah baris (\r\n) agar tidak merusak JSON JavaScript di HTML
                foreach (var h in headers)
                {
                    if (!string.IsNullOrEmpty(h.Remark))
                    {
                        h.Remark = h.Remark.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'").Replace("\\", "/");
                    }
                }

                RpoList = headers;

                // 4. Ambil rincian detail item untuk Header di atas
                if (RpoList.Any())
                {
                    var transNmbrs = RpoList.Select(h => h.TransNmbr).ToList();
                    var details = (await conn.QueryAsync<RpoDetailViewModel>(@"
                        SELECT TransNmbr, ProductCode, ProductPart, Qty, Unit, Remark, PriceForex, AmountForex, TotalForex
                        FROM STCRRPODt
                        WHERE TransNmbr IN @TransNmbrs",
                        new { TransNmbrs = transNmbrs }
                    )).ToList();

                    // Bersihkan teks remark di sisi detail item juga
                    foreach (var d in details)
                    {
                        if (!string.IsNullOrEmpty(d.Remark))
                        {
                            d.Remark = d.Remark.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'").Replace("\\", "/");
                        }
                    }

                    // Gabungkan data detail ke masing-masing header
                    foreach (var header in RpoList)
                    {
                        header.Details = details.Where(d => d.TransNmbr == header.TransNmbr).ToList();
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new JsonResult(new { success = true, redirect = "/Login" });
        }
    }

    public class RpoHeaderViewModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime TransDate { get; set; }
        public string PONo { get; set; } = string.Empty;
        public string SuppCode { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public decimal TotalForex { get; set; }
        public string UserPrep { get; set; } = string.Empty;
        public DateTime DatePrep { get; set; }

        public List<RpoDetailViewModel> Details { get; set; } = new();
    }

    public class RpoDetailViewModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductPart { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public decimal PriceForex { get; set; }
        public decimal AmountForex { get; set; }
        public decimal TotalForex { get; set; }
    }
}
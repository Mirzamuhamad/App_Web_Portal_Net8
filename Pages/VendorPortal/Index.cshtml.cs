using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using System.Data;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.VendorPortal
{
    public class IndexModel : PageModel
    {
        public string VendorName { get; set; } = string.Empty;
        public string VendorCode { get; set; } = string.Empty;
        public List<PoHeaderViewModel> PoList { get; set; } = new();
        public List<RpoHeaderViewModel> RpoList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            using var conn = Db.Connect();
            var vendor = await GetAuthenticatedVendorAsync(conn);
            if (vendor == null)
            {
                return RedirectToPage("/Login");
            }

            VendorName = vendor.Nama;
            VendorCode = vendor.SuppCode;

            PoList = await LoadPostedPoAsync(conn, VendorCode);
            RpoList = await LoadRpoAsync(conn, VendorCode);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateRrpoAsync(string poNo)
        {
            using var conn = Db.Connect();
            await conn.OpenAsync();

            var vendor = await GetAuthenticatedVendorAsync(conn);
            if (vendor == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return new JsonResult(new { success = false, message = "Sesi vendor tidak valid. Silakan login ulang." });
            }

            if (string.IsNullOrWhiteSpace(poNo))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new JsonResult(new { success = false, message = "Nomor PO tidak valid." });
            }

            using var trx = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var po = await conn.QueryFirstOrDefaultAsync<PoCreateModel>(@"
                    SELECT TOP 1 TransNmbr, Revisi, TransDate, Status, Supplier, Delivery, Currency, ForexRate,
                           BaseForex, DiscForex, PPnForex, PPhForex, TotalForex, Remark
                    FROM PRCPOHd
                    WHERE TransNmbr = @PoNo
                      AND Supplier = @SuppCode
                      AND FgActive = 'Y'
                      AND Status = 'P'
                      AND DatePost IS NOT NULL",
                    new { PoNo = poNo.Trim(), SuppCode = vendor.SuppCode },
                    trx
                );

                if (po == null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    return new JsonResult(new { success = false, message = "PO tidak ditemukan atau belum posting untuk vendor ini." });
                }

                var alreadyExists = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(1)
                    FROM STCRRPOHd
                    WHERE PONo = @PoNo
                      AND SuppCode = @SuppCode
                      AND ISNULL(Status, '') <> 'D'",
                    new { PoNo = po.TransNmbr, SuppCode = vendor.SuppCode },
                    trx
                );

                if (alreadyExists > 0)
                {
                    Response.StatusCode = StatusCodes.Status409Conflict;
                    return new JsonResult(new { success = false, message = "RRPO untuk PO ini sudah pernah dibuat." });
                }

                var detailCount = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(1)
                    FROM PRCPODt
                    WHERE TransNmbr = @PoNo
                      AND Revisi = @Revisi",
                    new { PoNo = po.TransNmbr, po.Revisi },
                    trx
                );

                if (detailCount == 0)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    return new JsonResult(new { success = false, message = "Detail item PO tidak ditemukan." });
                }

                var now = DateTime.Now;
                var rrpoNo = await GenerateRrpoNumberAsync(conn, trx, now);
                var userCode = vendor.SuppCode.Length > 30 ? vendor.SuppCode[..30] : vendor.SuppCode;
                var wrhsCode = string.IsNullOrWhiteSpace(po.Delivery) ? "01" : po.Delivery.Trim();
                if (wrhsCode.Length > 5)
                {
                    wrhsCode = wrhsCode[..5];
                }

                await conn.ExecuteAsync(@"
                    INSERT INTO STCRRPOHd
                    (
                        TransNmbr, Status, TransDate, PONo, SuppCode, FgHome, ShipTo, WrhsCode,
                        Remark, UserPrep, DatePrep, RRType, CurrCode, ForexRate, BaseForex,
                        DiscForex, PPNForex, PPhForex, TotalForex, FgProcess, DoneInvoice,
                        DoneDuty, DoneHandling, DoneDiscount, FgReport, TotalCost,
                        DoneBC, DoneCorrection, DonePurchaseCost, UserPost, DatePost
                    )
                    VALUES
                    (
                        @RrpoNo, 'H', @Now, @PoNo, @SuppCode, 'Y', @WrhsCode, @WrhsCode,
                        LEFT(@Remark, 60), @UserCode, @Now, 'RR', @Currency, @ForexRate, @BaseForex,
                        @DiscForex, @PPnForex, @PPhForex, @TotalForex, 'N', 'N',
                        'N', 'N', 'N', 'N', @TotalForex,
                        'N', 'N', 'N', @UserCode, @Now
                    );",
                    new
                    {
                        RrpoNo = rrpoNo,
                        Now = now,
                        PoNo = po.TransNmbr,
                        SuppCode = vendor.SuppCode,
                        WrhsCode = wrhsCode,
                        Remark = po.Remark ?? string.Empty,
                        UserCode = userCode,
                        Currency = po.Currency,
                        ForexRate = po.ForexRate,
                        BaseForex = po.BaseForex,
                        DiscForex = po.DiscForex,
                        PPnForex = po.PPnForex,
                        PPhForex = po.PPhForex,
                        TotalForex = po.TotalForex
                    },
                    trx
                );

                await conn.ExecuteAsync(@"
                    INSERT INTO STCRRPODt
                    (
                        TransNmbr, ProductCode, ProductPart, Qty, Unit, Remark, FgQC, FgQA,
                        PriceForex, AmountForex, DiscForex, TotalForex, TotalHome, TotalHPP,
                        HaveQC, QtyPacking, UnitPacking, QtySisa, JmlPacking, QtyPO
                    )
                    SELECT
                        @RrpoNo,
                        Product,
                        ' ',
                        Qty,
                        Unit,
                        LEFT(COALESCE(NULLIF(Remark, ''), NULLIF(Specification, ''), ''), 60),
                        'N',
                        '',
                        PriceForex,
                        BrutoForex,
                        DiscForex,
                        NettoForex,
                        NettoForex,
                        NettoForex,
                        'N',
                        Qty,
                        Unit,
                        0,
                        CASE WHEN ISNULL(QtyPack, 0) = 0 THEN 1 ELSE QtyPack END,
                        Qty
                    FROM PRCPODt
                    WHERE TransNmbr = @PoNo
                      AND Revisi = @Revisi;",
                    new { RrpoNo = rrpoNo, PoNo = po.TransNmbr, po.Revisi },
                    trx
                );

                await conn.ExecuteAsync(@"
                    UPDATE PRCPODt
                    SET QtyRR = Qty
                    WHERE TransNmbr = @PoNo
                      AND Revisi = @Revisi;",
                    new { PoNo = po.TransNmbr, po.Revisi },
                    trx
                );

                trx.Commit();
                return new JsonResult(new { success = true, rrpoNo, message = $"RRPO {rrpoNo} berhasil dibuat." });
            }
            catch (Exception ex)
            {
                trx.Rollback();
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new JsonResult(new { success = false, message = $"Gagal membuat RRPO: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new JsonResult(new { success = true, redirect = "/Login" });
        }

        private async Task<VendorUserInfo?> GetAuthenticatedVendorAsync(System.Data.IDbConnection conn)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdStr) || role != "VENDOR")
            {
                return null;
            }

            if (!int.TryParse(userIdStr, out int userId))
            {
                return null;
            }

            var user = await conn.QueryFirstOrDefaultAsync<VendorUserInfo>(@"
                SELECT Nama, SuppCode
                FROM PortalUsers
                WHERE UserId = @UserId",
                new { UserId = userId }
            );

            if (user == null || string.IsNullOrWhiteSpace(user.SuppCode))
            {
                return null;
            }

            user.Nama = string.IsNullOrWhiteSpace(user.Nama) ? user.SuppCode.Trim() : user.Nama.Trim();
            user.SuppCode = user.SuppCode.Trim();
            return user;
        }

        private static async Task<List<PoHeaderViewModel>> LoadPostedPoAsync(System.Data.IDbConnection conn, string suppCode)
        {
            var headers = (await conn.QueryAsync<PoHeaderViewModel>(@"
                SELECT h.TransNmbr, h.Revisi, h.TransDate, h.Status, h.Supplier, h.Attn, h.Currency,
                       h.TotalForex, h.Remark, h.UserPrep, h.DatePrep, h.UserPost, h.DatePost,
                       h.Delivery, h.DeliveryAddr, h.DeliveryCity,
                       ISNULL(d.TotalItems, 0) AS TotalItems,
                       ISNULL(d.TotalQty, 0) AS TotalQty,
                       CAST(CASE WHEN rr.TransNmbr IS NULL THEN 0 ELSE 1 END AS bit) AS HasRrpo,
                       rr.TransNmbr AS RrpoNo
                FROM PRCPOHd h
                OUTER APPLY (
                    SELECT COUNT(1) AS TotalItems, SUM(Qty) AS TotalQty
                    FROM PRCPODt
                    WHERE TransNmbr = h.TransNmbr
                      AND Revisi = h.Revisi
                ) d
                OUTER APPLY (
                    SELECT TOP 1 TransNmbr
                    FROM STCRRPOHd
                    WHERE PONo = h.TransNmbr
                      AND SuppCode = h.Supplier
                      AND ISNULL(Status, '') <> 'D'
                    ORDER BY DatePrep DESC, TransNmbr DESC
                ) rr
                WHERE h.Supplier = @SuppCode
                  AND h.FgActive = 'Y'
                  AND h.Status = 'P'
                  AND h.DatePost IS NOT NULL
                ORDER BY h.TransDate DESC, h.TransNmbr DESC",
                new { SuppCode = suppCode }
            )).ToList();

            if (headers.Count == 0)
            {
                return headers;
            }

            var poNos = headers.Select(h => h.TransNmbr).ToList();
            var details = (await conn.QueryAsync<PoDetailViewModel>(@"
                SELECT TransNmbr, Revisi, Product, Specification, QtyOrder, UnitOrder, Qty, Unit,
                       PriceForex, BrutoForex, Disc, DiscForex, NettoForex, Remark, QtyRR, QtyPack
                FROM PRCPODt
                WHERE TransNmbr IN @PoNos
                ORDER BY TransNmbr DESC, Product",
                new { PoNos = poNos }
            )).ToList();

            foreach (var header in headers)
            {
                header.Details = details.Where(d => d.TransNmbr == header.TransNmbr && d.Revisi == header.Revisi).ToList();
            }

            return headers;
        }

        private static async Task<List<RpoHeaderViewModel>> LoadRpoAsync(System.Data.IDbConnection conn, string suppCode)
        {
            var headers = (await conn.QueryAsync<RpoHeaderViewModel>(@"
                SELECT TransNmbr, Status, TransDate, PONo, SuppCode, Remark, TotalForex, UserPrep, DatePrep, UserPost, DatePost
                FROM STCRRPOHd
                WHERE SuppCode = @SuppCode
                  AND ISNULL(Status, '') <> 'D'
                ORDER BY TransDate DESC, TransNmbr DESC",
                new { SuppCode = suppCode }
            )).ToList();

            if (headers.Count == 0)
            {
                return headers;
            }

            var transNmbrs = headers.Select(h => h.TransNmbr).ToList();
            var details = (await conn.QueryAsync<RpoDetailViewModel>(@"
                SELECT TransNmbr, ProductCode, ProductPart, Qty, Unit, Remark, PriceForex, AmountForex, TotalForex
                FROM STCRRPODt
                WHERE TransNmbr IN @TransNmbrs
                ORDER BY TransNmbr DESC, ProductCode",
                new { TransNmbrs = transNmbrs }
            )).ToList();

            foreach (var header in headers)
            {
                header.Details = details.Where(d => d.TransNmbr == header.TransNmbr).ToList();
                header.TotalItems = header.Details.Count;
            }

            return headers;
        }

        private static async Task<string> GenerateRrpoNumberAsync(System.Data.IDbConnection conn, IDbTransaction trx, DateTime date)
        {
            var prefix = $"IGL/RPO/{date:yy-MM}/";
            var lastNumber = await conn.ExecuteScalarAsync<int?>(@"
                SELECT MAX(
    CASE 
        WHEN ISNUMERIC(RIGHT(TransNmbr, 4)) = 1 
        THEN CONVERT(INT, RIGHT(TransNmbr, 4)) 
        ELSE 0 
    END
)
FROM STCRRPOHd WITH (UPDLOCK, HOLDLOCK)
WHERE TransNmbr LIKE @Prefix + '%'",
                new { Prefix = prefix },
                trx
            ) ?? 0;

            var next = lastNumber + 1;
            string candidate;
            do
            {
                candidate = $"{prefix}{next:0000}";
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM STCRRPOHd WHERE TransNmbr = @Candidate",
                    new { Candidate = candidate },
                    trx
                );

                if (exists == 0)
                {
                    return candidate;
                }

                next++;
            }
            while (next <= 9999);

            throw new InvalidOperationException("Nomor RRPO bulan ini sudah mencapai batas maksimum.");
        }
    }

    public class VendorUserInfo
    {
        public string Nama { get; set; } = string.Empty;
        public string SuppCode { get; set; } = string.Empty;
    }

    public class PoCreateModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public int Revisi { get; set; }
        public DateTime TransDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string? Delivery { get; set; }
        public string Currency { get; set; } = "Rp";
        public decimal ForexRate { get; set; }
        public decimal BaseForex { get; set; }
        public decimal DiscForex { get; set; }
        public decimal? PPnForex { get; set; }
        public decimal? PPhForex { get; set; }
        public decimal TotalForex { get; set; }
        public string? Remark { get; set; }
    }

    public class PoHeaderViewModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public int Revisi { get; set; }
        public DateTime TransDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string? Attn { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal TotalForex { get; set; }
        public string? Remark { get; set; }
        public string? UserPrep { get; set; }
        public DateTime? DatePrep { get; set; }
        public string? UserPost { get; set; }
        public DateTime? DatePost { get; set; }
        public string? Delivery { get; set; }
        public string? DeliveryAddr { get; set; }
        public string? DeliveryCity { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalQty { get; set; }
        public bool HasRrpo { get; set; }
        public string? RrpoNo { get; set; }
        public List<PoDetailViewModel> Details { get; set; } = new();
    }

    public class PoDetailViewModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public int Revisi { get; set; }
        public string Product { get; set; } = string.Empty;
        public string? Specification { get; set; }
        public decimal QtyOrder { get; set; }
        public string UnitOrder { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal PriceForex { get; set; }
        public decimal BrutoForex { get; set; }
        public decimal Disc { get; set; }
        public decimal DiscForex { get; set; }
        public decimal NettoForex { get; set; }
        public string? Remark { get; set; }
        public decimal? QtyRR { get; set; }
        public decimal? QtyPack { get; set; }
    }

    public class RpoHeaderViewModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime TransDate { get; set; }
        public string PONo { get; set; } = string.Empty;
        public string SuppCode { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public decimal TotalForex { get; set; }
        public string? UserPrep { get; set; }
        public DateTime? DatePrep { get; set; }
        public string? UserPost { get; set; }
        public DateTime? DatePost { get; set; }
        public int TotalItems { get; set; }
        public List<RpoDetailViewModel> Details { get; set; } = new();
    }

    public class RpoDetailViewModel
    {
        public string TransNmbr { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductPart { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public decimal? PriceForex { get; set; }
        public decimal? AmountForex { get; set; }
        public decimal? TotalForex { get; set; }
    }
}

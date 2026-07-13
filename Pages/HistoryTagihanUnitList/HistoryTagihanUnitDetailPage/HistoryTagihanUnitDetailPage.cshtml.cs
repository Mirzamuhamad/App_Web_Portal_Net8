using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList.HistoryTagihanUnitDetailPage
{
    public class HistoryTagihanUnitDetailPageModel : PageModel
    {
        public KavlingInfo Unit { get; set; } = new();
        public List<KavlingInfoDetail> UnitDetailTagihan { get; set; } = new();
        public decimal TotalSemuaTagihan => UnitDetailTagihan?.Sum(x => x.AmountPerKavling) ?? 0;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Login");
            }

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();

                const string sqlUnit = @"
                    SELECT KavlingId, KavlingCode, Kawasan, Luas
                    FROM V_ListKavlingUserPOrtal
                    WHERE KavlingId = @KavlingId AND UserId = @UserId;";

                var unit = await connection.QueryFirstOrDefaultAsync<KavlingInfo>(sqlUnit, new { KavlingId = id, UserId = userId });

                if (unit == null)
                {
                    return RedirectToPage("/Index");
                }

                Unit = unit;

                const string sqlUnitDetail = @"
                    SELECT *
                    FROM V_GetTagihanDetailKavling
                    WHERE UserId = @UserId
                        AND KavlingId = @KavlingId
                       
                    ORDER BY DueDate DESC, TransNmbr DESC";

                var resultDetail = await connection.QueryAsync<KavlingInfoDetail>(sqlUnitDetail, new { UserId = userId, KavlingId = id });
                UnitDetailTagihan = resultDetail.ToList();
            }

            return Page();
        }

        public class KavlingInfo
        {
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; } = string.Empty;
            public string Kawasan { get; set; } = string.Empty;
            public decimal Luas { get; set; }
        }

        public class KavlingInfoDetail
        {
            public int KavlingId { get; set; }
            public string TransNmbr { get; set; } = string.Empty;
            public string CustCode { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public DateTime? DueDate { get; set; }
            public string CommercialItem { get; set; } = string.Empty;
            public string CommercialDesc { get; set; } = string.Empty;
            public decimal AmountPerKavling { get; set; }
            public decimal TotalAmountKavling { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}

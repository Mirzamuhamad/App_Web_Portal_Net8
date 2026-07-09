using Dapper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList
{
    public class HistoryTagihanUnitListModel : PageModel
    {
        public List<UnitItemList> UnitItems { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                Response.Redirect("/Login");
                return;
            }

            using (var connection = Db.Connect())
            {
                const string sqlUnits = "SELECT * FROM V_ListKavlingUserPOrtal WHERE UserId = @Id";
                var units = (await connection.QueryAsync<UnitItemList>(sqlUnits, new { Id = userId })).ToList();

                const string sqlPaidSummary = @"
                    SELECT
                        KavlingId,
                        COUNT(DISTINCT TransNmbr) AS PaidInvoiceCount,
                        SUM(AmountPerKavling) AS TotalPaidAmount
                    FROM V_GetTagihanDetailKavling
                    WHERE UserId = @Id AND Status = 'P'
                    GROUP BY KavlingId";

                var paidSummary = (await connection.QueryAsync<HistoryPaidSummary>(sqlPaidSummary, new { Id = userId }))
                    .ToDictionary(x => x.KavlingId.ToString(), x => x);

                foreach (var unit in units)
                {
                    if (paidSummary.TryGetValue(unit.KavlingId, out var summary))
                    {
                        unit.PaidInvoiceCount = summary.PaidInvoiceCount;
                        unit.TotalPaidAmount = summary.TotalPaidAmount;
                    }
                }

                UnitItems = units.Where(x => x.PaidInvoiceCount > 0).ToList();
            }
        }
    }

    public class UnitItemList
    {
        public string KavlingId { get; set; } = string.Empty;
        public string KavlingCode { get; set; } = string.Empty;
        public string kawasan { get; set; } = string.Empty;
        public string ImagePath { get; set; } = "/Image/image9.jpg";
        public string OwnerType { get; set; } = string.Empty;
        public string TicketKavlingCount { get; set; } = string.Empty;
        public decimal TotalAmountKavling { get; set; }
        public decimal Luas { get; set; }
        public int PaidInvoiceCount { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }

    public class HistoryPaidSummary
    {
        public int KavlingId { get; set; }
        public int PaidInvoiceCount { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }
}

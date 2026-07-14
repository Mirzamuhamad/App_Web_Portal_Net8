using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList
{
    public class HistoryTagihanUnitListModel : PageModel
    {
        // 1. Tambahkan properti untuk menangkap filter tanggal dari UI
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

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

                // 2. Sesuaikan query SQL untuk mendukung filter rentang tanggal
                // Catatan: Ganti 'PaymentDate' sesuai dengan nama kolom tanggal aktual di database Anda
                string sqlPaidSummary = @"
                    SELECT
                        KavlingId,
                        COUNT(DISTINCT TransNmbr) AS PaidInvoiceCount,
                        SUM(AmountPerKavling) AS TotalPaidAmount
                    FROM V_GetTagihanDetailKavlingHistory
                    WHERE UserId = @Id  ";

                if (StartDate.HasValue)
                {
                    sqlPaidSummary += " AND DueDate >= @StartDate ";
                }
                if (EndDate.HasValue)
                {
                    sqlPaidSummary += " AND PaymentDate <= @EndDate ";
                }

                sqlPaidSummary += " GROUP BY KavlingId";

                var parameters = new DynamicParameters();
                parameters.Add("Id", userId);
                if (StartDate.HasValue) parameters.Add("StartDate", StartDate.Value);
                if (EndDate.HasValue) parameters.Add("EndDate", EndDate.Value.Date.AddDays(1).AddTicks(-1)); // Agar mencakup akhir hari penuh

                var paidSummary = (await connection.QueryAsync<HistoryPaidSummary>(sqlPaidSummary, parameters))
                    .ToDictionary(x => x.KavlingId.ToString(), x => x);

                foreach (var unit in units)
                {
                    if (paidSummary.TryGetValue(unit.KavlingId, out var summary))
                    {
                        unit.PaidInvoiceCount = summary.PaidInvoiceCount;
                        unit.TotalPaidAmount = summary.TotalPaidAmount;
                    }
                }

                // 3. Batasi hanya 10 item teratas (diurutkan berdasarkan total nominal terbesar atau kondisi lain)
                UnitItems = units
                    .Where(x => x.PaidInvoiceCount > 0)
                    .OrderByDescending(x => x.TotalPaidAmount) // Bisa disesuaikan pengurutannya
                    .Take(10)
                    .ToList();
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
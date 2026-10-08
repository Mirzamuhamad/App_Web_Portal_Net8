using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList
{
    public class HistoryTagihanUnitListModel : PageModel
    {
        private const int PageSize = 10;

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int CurrentPage { get; set; } = 1;

        public int TotalInvoiceCount { get; private set; }
        public int TotalPages { get; private set; } = 1;
        public List<PaidInvoice> Invoices { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return RedirectToPage("/Login");
            }

            if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
            {
                (StartDate, EndDate) = (EndDate, StartDate);
            }

            var startDate = StartDate?.Date;
            var endDateExclusive = EndDate?.Date.AddDays(1);

            const string receiptCte = @"
                WITH ReceiptCandidates AS
                (
                    SELECT
                        D.InvoiceNo,
                        E.TransNmbr AS ReceiptNo,
                        COALESCE(E.ReceiptDate, E.TransDate) AS PaymentDate,
                        dbo.Dashboard_Url(D.FileFakturPajak) AS FileFakturPajak,
                        dbo.Dashboard_Url(E.FileKwitansi) AS FileKwitansi,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY D.InvoiceNo
                            ORDER BY COALESCE(E.ReceiptDate, E.TransDate) DESC, E.TransNmbr DESC
                        ) AS RowNo
                    FROM FINReceiptTradeHd E
                    INNER JOIN FINReceiptTradeDtInv D ON D.TransNmbr = E.TransNmbr
                    WHERE E.Status = 'P'
                ),
                PaidInvoices AS
                (
                    SELECT InvoiceNo, ReceiptNo, PaymentDate, FileFakturPajak, FileKwitansi
                    FROM ReceiptCandidates
                    WHERE RowNo = 1
                ) ";

            const string filterSql = @"
                WHERE A.Status = 'P'
                  AND EXISTS
                  (
                      SELECT 1
                      FROM V_PortalUsers U
                      WHERE U.UserId = @UserId
                        AND U.CustCode = A.CustCode
                  )
                  AND (@StartDate IS NULL OR P.PaymentDate >= @StartDate)
                  AND (@EndDateExclusive IS NULL OR P.PaymentDate < @EndDateExclusive) ";

            var parameters = new
            {
                UserId = userId,
                StartDate = startDate,
                EndDateExclusive = endDateExclusive
            };

            using var connection = Db.Connect();
            await connection.OpenAsync();

            TotalInvoiceCount = await connection.QuerySingleAsync<int>(
                receiptCte + @"
                SELECT COUNT(*)
                FROM TenantBillingInvoiceHd A
                INNER JOIN PaidInvoices P ON P.InvoiceNo = A.TransNmbr " + filterSql,
                parameters);

            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalInvoiceCount / (double)PageSize));
            CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

            var headerParameters = new
            {
                UserId = userId,
                StartDate = startDate,
                EndDateExclusive = endDateExclusive,
                Offset = (CurrentPage - 1) * PageSize,
                PageSize
            };

            var headers = (await connection.QueryAsync<PaidInvoice>(
                receiptCte + @"
                SELECT
                    A.TransNmbr AS InvoiceNo,
                    P.ReceiptNo,
                    P.PaymentDate,
                    A.DueDate,
                    A.CustCode,
                    P.FileFakturPajak,
                    P.FileKwitansi,
                    COALESCE(T.TotalAmount, 0) AS TotalAmount
                FROM TenantBillingInvoiceHd A
                INNER JOIN PaidInvoices P ON P.InvoiceNo = A.TransNmbr
                LEFT JOIN
                (
                    SELECT TransNmbr, SUM(AmountForex) AS TotalAmount
                    FROM TenantBillingInvoiceDt
                    GROUP BY TransNmbr
                ) T ON T.TransNmbr = A.TransNmbr " + filterSql + @"
                ORDER BY P.PaymentDate DESC, A.TransNmbr DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;",
                headerParameters)).ToList();

            if (headers.Count == 0)
            {
                return Page();
            }

            var invoiceNumbers = headers.Select(x => x.InvoiceNo).ToArray();
            const string detailSql = @"
                SELECT
                    D.TransNmbr AS InvoiceNo,
                    D.KavlingId,
                    COALESCE(NULLIF(LTRIM(RTRIM(K.KavlingCode)), ''), CAST(D.KavlingId AS varchar(20))) AS KavlingCode,
                    COALESCE(K.Luas, 0) AS Luas,
                    D.CommercialItem,
                    D.CommercialDesc,
                    D.AmountForex AS AmountPerKavling
                FROM TenantBillingInvoiceDt D
                LEFT JOIN MsKavlingsPortal K ON K.KavlingId = D.KavlingId
                WHERE D.TransNmbr IN @InvoiceNumbers
                ORDER BY D.TransNmbr, D.KavlingId, D.CommercialItem;";

            var details = (await connection.QueryAsync<PaidInvoiceDetail>(
                detailSql,
                new { InvoiceNumbers = invoiceNumbers })).ToList();

            var detailsByInvoice = details
                .GroupBy(x => x.InvoiceNo)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var invoice in headers)
            {
                if (detailsByInvoice.TryGetValue(invoice.InvoiceNo, out var invoiceDetails))
                {
                    invoice.Details = invoiceDetails;
                }
            }

            Invoices = headers;
            return Page();
        }

        public class PaidInvoice
        {
            public string InvoiceNo { get; set; } = string.Empty;
            public string ReceiptNo { get; set; } = string.Empty;
            public DateTime? PaymentDate { get; set; }
            public DateTime? DueDate { get; set; }
            public string CustCode { get; set; } = string.Empty;
            public string FileFakturPajak { get; set; } = string.Empty;
            public string FileKwitansi { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public List<PaidInvoiceDetail> Details { get; set; } = new();
        }

        public class PaidInvoiceDetail
        {
            public string InvoiceNo { get; set; } = string.Empty;
            public int KavlingId { get; set; }
            public string KavlingCode { get; set; } = string.Empty;
            public decimal Luas { get; set; }
            public string CommercialItem { get; set; } = string.Empty;
            public string CommercialDesc { get; set; } = string.Empty;
            public decimal AmountPerKavling { get; set; }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using System.Data;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.TagihanUnitDetailPage
{
    [AllowAnonymous]
    public class TagihanUnitInvoicePrintModel : PageModel
    {
        public InvoiceHeaderData InvoiceHeader { get; set; } = new();
        public List<InvoiceItemData> InvoiceItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? invoiceNo, string? kavlingCode, int? userId)
        {
            invoiceNo = invoiceNo?.Trim();
            kavlingCode = kavlingCode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(invoiceNo))
            {
                return BadRequest("Nomor invoice tidak valid.");
            }

            // Aplikasi lama VB.NET mengirim userId lewat query string karena tidak
            // memiliki cookie portal. Akses internal tetap dapat memakai claim login.
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int resolvedUserId;

            if (userId.HasValue && userId.Value > 0)
            {
                resolvedUserId = userId.Value;
            }
            else if (int.TryParse(userIdStr, out int authenticatedUserId))
            {
                resolvedUserId = authenticatedUserId;
            }
            else
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Content = "UserId tidak valid atau tidak ditemukan."
                };
            }

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Nmbr", invoiceNo, DbType.String);
                parameters.Add("@UserId", resolvedUserId, DbType.Int32);
                parameters.Add("@Kavling", kavlingCode, DbType.String);

                using (var multi = await connection.QueryMultipleAsync("S_FnFormBillingTenantInvoice", parameters, commandType: CommandType.StoredProcedure))
                {
                    if (!multi.IsConsumed)
                    {
                        // Membaca data flat sesuai struktur kolom SQL asli
                        var items = (await multi.ReadAsync<InvoiceFlatModel>()).ToList();
                        
                        if (items.Any())
                        {
                            var first = items.First();
                            InvoiceHeader = new InvoiceHeaderData
                            {
                                TransNmbr = first.TransNmbr,
                                DueDate = first.DueDate,
                                CustomerName = first.Customer_Name, // Diubah sesuai kolom database
                                KavlingCode = first.KavlingCode,
                                Address = first.Address,
                                PeriodeDesc = first.CommercialDesc, // Menggunakan deskripsi utama sebagai periode
                                Bank = first.Bank,                  // Diubah sesuai kolom database
                                Rekening = first.Rekening,          // Diubah sesuai kolom database
                                BaseForex = first.BaseForex,        // Diambil langsung dari kolom database akumulasi
                                PPnForex = first.PPnForex,          // Diambil langsung dari kolom database akumulasi
                                TotalForex = first.TotalForex,
                                CompanyName = first.CompanyName,
                                PPn = first.PPn,            // Diambil langsung dari kolom database akumulasi
                                TransDate = first.TransDate // Diambil langsung dari kolom database
                            };

                            InvoiceItems = items.Select(x => new InvoiceItemData
                            {
                                Luas = x.Luas,
                                CommercialItem = x.CommercialItem,
                                DeskripsiItemCommercial = x.DeskripsiItemCommercial, // Diubah sesuai kolom database
                                NettoForex = x.NettoForex                            // Diubah sesuai kolom database
                            }).ToList();
                        }
                    }
                }
            }

            if (InvoiceHeader == null || string.IsNullOrEmpty(InvoiceHeader.TransNmbr))
            {
                return Content("Data Invoice tidak ditemukan atau Anda tidak memiliki akses.");
            }

            return Page();
        }

        // Penampung mapping data flat (NAMA PROPERTI HARUS SAMA PERSIS DENGAN KOLOM DATABASE)
        public class InvoiceFlatModel
        {
            public string TransNmbr { get; set; }
            public DateTime? TransDate { get; set; }
            public string CustCode { get; set; }
            public string KavlingId { get; set; }
            public string CommercialItem { get; set; }
            public string CommercialDesc { get; set; }
            public string BillingID { get; set; }
            public decimal Qty { get; set; }
            public decimal Price { get; set; }
            public decimal NettoForex { get; set; }
            public decimal BaseForex { get; set; }
            public decimal PPnForex { get; set; }
            public decimal TotalForex { get; set; }
            public DateTime? DueDate { get; set; }
            public string Rekening { get; set; }
            public string Bank { get; set; }
            public string CompanyName { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string StatusName { get; set; }
            public string KavlingCode { get; set; }
            public string Customer_Name { get; set; } // Pakai Underscore sesuai SQL
            public string Reference { get; set; }
            public decimal Luas { get; set; }
            public string DeskripsiItemCommercial { get; set; }
            public string BillingModel { get; set; }

            public decimal PPn { get; set; }
        }

        public class InvoiceHeaderData
        {
            public string TransNmbr { get; set; }
            public DateTime? TransDate { get; set; }
            public DateTime? DueDate { get; set; }
            public string CustomerName { get; set; }
            public string KavlingCode { get; set; }
            public string Address { get; set; }
            public string PeriodeDesc { get; set; }
            public string Bank { get; set; }
            public string Rekening { get; set; }
            public decimal BaseForex { get; set; }
            public decimal PPnForex { get; set; }
            public decimal TotalForex { get; set; }

            public decimal PPn { get; set; }

            public string CompanyName { get; set; }
        }

        public class InvoiceItemData
        {
            public decimal Luas { get; set; }
            public string DeskripsiItemCommercial { get; set; }
            public string CommercialItem { get; set; }
            public decimal NettoForex { get; set; }
        }
    }
}

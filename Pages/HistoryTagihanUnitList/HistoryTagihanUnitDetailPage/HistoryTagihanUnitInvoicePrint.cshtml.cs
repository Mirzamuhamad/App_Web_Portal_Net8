using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList.HistoryTagihanUnitDetailPage
{
    public class HistoryTagihanUnitInvoicePrintModel : PageModel
    {
        public InvoiceHeaderData InvoiceHeader { get; set; } = new();
        public List<InvoiceItemData> InvoiceItems { get; set; } = new();
        public string DocumentType { get; set; } = "kwitansi";
        public string DocumentTitle => DocumentType switch
        {
            "faktur-pajak" => "FAKTUR PAJAK",
            "kwitansi" => "KWITANSI PEMBAYARAN",
            _ => "KWITANSI PEMBAYARAN"
        };
        public string FilePrefix => DocumentType switch
        {
            "faktur-pajak" => "Faktur-Pajak",
            "kwitansi" => "Kwitansi",
            _ => "Kwitansi"
        };

        public async Task<IActionResult> OnGetAsync(string invoiceNo, string kavlingCode, string documentType = "kwitansi")
        {
            if (string.IsNullOrEmpty(invoiceNo))
            {
                return RedirectToPage("/Index");
            }

            DocumentType = documentType == "faktur-pajak" ? "faktur-pajak" : "kwitansi";

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Login");
            }

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Nmbr", invoiceNo, DbType.String);
                parameters.Add("@UserId", userId, DbType.Int32);
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
                                PPn = first.PPn            // Diambil langsung dari kolom database akumulasi
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
            public string TransNmbr { get; set; } = string.Empty;
            public string CustCode { get; set; } = string.Empty;
            public string KavlingId { get; set; } = string.Empty;
            public string CommercialItem { get; set; } = string.Empty;
            public string CommercialDesc { get; set; } = string.Empty;
            public string BillingID { get; set; } = string.Empty;
            public decimal Qty { get; set; }
            public decimal Price { get; set; }
            public decimal NettoForex { get; set; }
            public decimal BaseForex { get; set; }
            public decimal PPnForex { get; set; }
            public decimal TotalForex { get; set; }
            public DateTime? DueDate { get; set; }
            public string Rekening { get; set; } = string.Empty;
            public string Bank { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string StatusName { get; set; } = string.Empty;
            public string KavlingCode { get; set; } = string.Empty;
            public string Customer_Name { get; set; } = string.Empty; // Pakai Underscore sesuai SQL
            public string Reference { get; set; } = string.Empty;
            public decimal Luas { get; set; }
            public string DeskripsiItemCommercial { get; set; } = string.Empty;
            public string BillingModel { get; set; } = string.Empty;

            public decimal PPn { get; set; }
        }

        public class InvoiceHeaderData
        {
            public string TransNmbr { get; set; } = string.Empty;
            public DateTime? DueDate { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string KavlingCode { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string PeriodeDesc { get; set; } = string.Empty;
            public string Bank { get; set; } = string.Empty;
            public string Rekening { get; set; } = string.Empty;
            public decimal BaseForex { get; set; }
            public decimal PPnForex { get; set; }
            public decimal TotalForex { get; set; }

            public decimal PPn { get; set; }

            public string CompanyName { get; set; } = string.Empty;
        }

        public class InvoiceItemData
        {
            public decimal Luas { get; set; }
            public string DeskripsiItemCommercial { get; set; } = string.Empty;
            public string CommercialItem { get; set; } = string.Empty;
            public decimal NettoForex { get; set; }
        }
    }
}

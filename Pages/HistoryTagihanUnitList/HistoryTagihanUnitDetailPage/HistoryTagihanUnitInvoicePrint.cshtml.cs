using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;

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
            "kwitansi" => "KWITANSI",
            _ => "KWITANSI"
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

            using (var connection = Db.Connect())
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@TransNmbr", invoiceNo, DbType.String);

                using (var multi = await connection.QueryMultipleAsync("S_Getkwitansi", parameters, commandType: CommandType.StoredProcedure))
                {
                    var items = (await multi.ReadAsync<InvoiceFlatModel>()).ToList();
                    
                    if (items.Any())
                    {
                        var first = items.First();
                        InvoiceHeader = new InvoiceHeaderData
                        {
                            TransNmbr = first.TransNmbr,
                            DueDate = first.DueDate,
                            CustomerName = first.Nama, 
                            KavlingCode = first.KavlingCode,
                            TotalBayar = first.TotalBayar,
                            CompanyName = first.CompanyName,
                            Address = first.Address,
                            TerbilangText = GetTerbilangString(first.TotalBayar),
                            InvoiceNumber = first.InvoiceNo,
                            Luas = first.Luas
                        };

                        InvoiceItems = items.Select(x => new InvoiceItemData
                        {
                            TransNmbr = x.InvoiceNo, // <-- Tambahan mapping
                            CommercialItem = x.CommercialItem,
                            CommercialDesc = x.CommercialDesc, 
                            KavlingCode = x.KavlingCode,
                            Luas = x.Luas,
                            AmountPerKavling = x.AmountPerKavling,
                            TotalAmountKavling = x.TotalAmountKavling
                        }).ToList();
                    }
                }
            }

            if (InvoiceHeader == null || string.IsNullOrEmpty(InvoiceHeader.TransNmbr))
            {
                return Content("Data Invoice tidak ditemukan atau Anda tidak memiliki akses.");
            }

            return Page();
        }

        // Helper Terbilang Rupiah
        public string Terbilang(decimal nilai)
        {
            string[] bilangan = { "", "Satu", "Dua", "Tiga", "Empat", "Lima", "Enam", "Tujuh", "Delapan", "Sembilan", "Sepuluh", "Sebelas" };
            if (nilai < 12)
                return " " + bilangan[(int)nilai];
            else if (nilai < 20)
                return Terbilang(nilai - 10) + " Belas";
            else if (nilai < 100)
                return Terbilang(nilai / 10) + " Puluh" + Terbilang(nilai % 10);
            else if (nilai < 200)
                return " Seratus" + Terbilang(nilai - 100);
            else if (nilai < 1000)
                return Terbilang(nilai / 100) + " Ratus" + Terbilang(nilai % 100);
            else if (nilai < 2000)
                return " Seribu" + Terbilang(nilai - 1000);
            else if (nilai < 1000000)
                return Terbilang(nilai / 1000) + " Ribu" + Terbilang(nilai % 1000);
            else if (nilai < 1000000000)
                return Terbilang(nilai / 1000000) + " Juta" + Terbilang(nilai % 1000000);
            else if (nilai < 1000000000000)
                return Terbilang(nilai / 1000000000) + " Miliar" + Terbilang(nilai % 1000000000);
            return "";
        }

        public string GetTerbilangString(decimal nominal)
        {
            if (nominal == 0) return "Nol Rupiah";
            string hasil = Terbilang(nominal).Trim();
            return hasil + " Rupiah";
        }

        public class InvoiceFlatModel
        {
            public string TransNmbr { get; set; } = string.Empty;
            public string Nama { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public DateTime? DueDate { get; set; }
            public string KavlingId { get; set; } = string.Empty;
            public string KavlingCode { get; set; } = string.Empty;
            public string CommercialItem { get; set; } = string.Empty;
            public string CommercialDesc { get; set; } = string.Empty;
            public decimal AmountPerKavling { get; set; }
            public decimal TotalAmountKavling { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal TotalBayar { get; set; }
            public string CompanyName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
             public string InvoiceNo { get; set; } = string.Empty;
              public decimal Luas { get; set; }
        }

        public class InvoiceHeaderData
        {
            public string TransNmbr { get; set; } = string.Empty;
            public DateTime? DueDate { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string KavlingCode { get; set; } = string.Empty;
            public decimal TotalBayar { get; set; }
            public string CompanyName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string TerbilangText { get; set; } = string.Empty;
            public string InvoiceNumber { get; set; } = string.Empty;
             public decimal Luas { get; set; }
        }

        public class InvoiceItemData
        {
            public string TransNmbr { get; set; } = string.Empty;
            public string CommercialItem { get; set; } = string.Empty;
            public string CommercialDesc { get; set; } = string.Empty;

            public string KavlingCode { get; set; } = string.Empty;
            public decimal Luas { get; set; }
            public decimal AmountPerKavling { get; set; }
            public decimal TotalAmountKavling { get; set; }

        }
    }
}
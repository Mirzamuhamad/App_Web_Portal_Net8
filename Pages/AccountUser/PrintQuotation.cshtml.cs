using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using TestLandingPageNet8.Pages.AccountUser; // Sesuaikan dengan namespace tempat ServiceOrderViewModel berada

namespace TestLandingPageNet8.Pages.AccountUser // Pastikan sesuai folder
{
public class PrintQuotationModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory; // Sesuaikan dengan factory Anda

    public ServiceOrderViewModel Data { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        using var connection = Db.Connect();
        
        // 1. Get Header
       // 1. Ubah ServiceId menjadi TransNmbr di query Header
var sqlHeader = "SELECT * FROM V_ServiceOrderList WHERE TransNmbr = @Id"; // <-- Ganti ini
Data = await connection.QueryFirstOrDefaultAsync<ServiceOrderViewModel>(sqlHeader, new { Id = id });

if (Data == null) return NotFound();

// 2. Ubah ServiceId menjadi TransNmbr di query Detail
var sqlDetails = "SELECT * FROM V_GetServiceOrderListDt WHERE TransNmbr = @Id"; // <-- Ganti ini
var details = (await connection.QueryAsync<ServiceOrderDetail>(sqlDetails, new { Id = id })).ToList();
        
        Data.SubDetails = details;

        return Page();
    }
}
}
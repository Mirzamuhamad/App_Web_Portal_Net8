using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestLandingPageNet8.Pages.Transportasi
{
    public class TransportasiModel : PageModel
    {
        public string PageTitle { get; set; } = "Transportasi Terdekat";

        public void OnGet()
        {
            // Anda bisa menambahkan logika otentikasi tenant di sini
        }
    }
}
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestLandingPageNet8.Pages.Ssos
{
    public class SsosModel : PageModel
    {
        public string PageTitle { get; set; } = "Layanan Darurat Terdekat";

        public void OnGet()
        {
            // Logika tambahan dapat ditambahkan di sini
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestLandingPageNet8.Pages.TagihanUnitList
{
    public class TagihanUnitListModel : PageModel
    {
        public List<InvoiceListItem> Invoices { get; set; } = new();

        public IActionResult OnGet()
        {
            return RedirectToPage(
                "/TagihanUnitList/TagihanUnitDetailPage/TagihanUnitDetailPage");
        }
    }

    public class InvoiceListItem
    {
        public string TransNmbr { get; set; } = string.Empty;
        public string CustCode { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestLandingPageNet8.Pages.UnitList
{
    public class UnitListModel : PageModel
    {
        public List<UnitItemList> UnitItems { get; set; } = new();

        public void OnGet()
        {
            LoadUnitItems();
        }

        private void LoadUnitItems()
        {
            UnitItems = new List<UnitItemList>
        {
            new UnitItemList { UnitCode = "B-KAV001", Building = "Area Kawasan Industri Cileles", ImagePath = "/Image/image1.png", Status = "Penyewa", TicketCount = 5 },
            new UnitItemList { UnitCode = "B-KAV002", Building = "Area Kawasan Industri Cileles B", ImagePath = "/Image/image2.png", Status = "Pemilik", TicketCount = 2 },
            new UnitItemList { UnitCode = "B-KAV003", Building = "Area Kawasan Industri Cileles C", ImagePath = "/Image/image3.png", Status = "Pemilik", TicketCount = 1 },
            new UnitItemList { UnitCode = "B-KAV004", Building = "Area Kawasan Industri Cileles D", ImagePath = "/Image/image4.png", Status = "Penyewa", TicketCount = 0 }
        };
        }
    }

    public class UnitItemList
    {
        public string UnitCode { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TicketCount { get; set; }
    }
}

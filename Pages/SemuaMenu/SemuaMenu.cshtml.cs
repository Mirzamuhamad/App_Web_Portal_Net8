using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;

namespace TestLandingPageNet8.Pages.SemuaMenu
{
    public class SemuaMenuModel : PageModel
    {
        public List<SemuaMenuItem> SemuaMenuItems { get; set; } = new();

        public void OnGet()
        {
            // untuk ambil local harcode data
            // LoadItemsMenu();
            // ViewData["SemuaMenu"] = SemuaMenuItems;

            // untuk ambil menu data dari database
            SemuaMenuItems = LoadItemsMenu();
            ViewData["SemuaMenu"] = SemuaMenuItems;
        }

        private List<SemuaMenuItem> LoadItemsMenu()
        {
            using (var conn = Db.Connect())
            {
                string sql = @"
            SELECT
                Title,
                IconPath,
                Url,
                Category
            FROM V_AppMenuPortal
            WHERE  COALESCE(Category,'') <> ''
        ";

                return conn.Query<SemuaMenuItem>(sql).ToList();
            }
        }

        // private void LoadItemsMenu()
        // {
        //     SemuaMenuItems = new List<SemuaMenuItem>
        //     {
        //        // --- Properti ---
        //         new SemuaMenuItem { Title="Unit Saya", IconPath="/icons/home.png", Url="/UnitList/UnitList", Category="Properti" },
        //         new SemuaMenuItem { Title="For Sale or Lease", IconPath="/icons/house.png", Url="/acara", Category="Properti" },
        //         new SemuaMenuItem { Title="Complaint", IconPath="/icons/complaint.png", Url="/CreateComplaint/CreateComplaint", Category="Properti" },
        //         new SemuaMenuItem { Title="Asset Booking", IconPath="/icons/asset.png", Url="/semua", Category="Properti" },
        //         new SemuaMenuItem { Title="Tagihan", IconPath ="/icons/bill.png", Url="/transportasi" , Category="Properti" },

        //         // --- Service ---
        //         new SemuaMenuItem { Title="Service Order", IconPath="/icons/mechanic.png", Url="/darurat", Category="Service" },
        //         new SemuaMenuItem { Title="Izin & Upload", IconPath="/icons/hand-paper-check.png", Url="/unit", Category="Service" },


        //         // --- Keamanan ---
        //         // new SemuaMenuItem { Title="CCTV", IconPath="/icons/cctv.png", Url="/cctv", Category="Keamanan" },
        //         new SemuaMenuItem { Title="Darurat", IconPath="/icons/sos.png", Url="/darurat", Category="Keamanan" },
        //         new SemuaMenuItem { Title="Pelanggaran", IconPath="/icons/handcuffs.png", Url="/cctv", Category="Keamanan" },

        //         // --- Informasi ---
        //         new SemuaMenuItem { Title="FAQ", IconPath="/icons/faq.png", Url="/properti", Category="Informasi" },
        //         new SemuaMenuItem { Title="Information Center", IconPath="/icons/info.png", Url="/unit", Category="Informasi" },

        //         // --- Lainnya ---  
        //         new SemuaMenuItem { Title="Acara", IconPath ="/icons/diagram.png", Url="/acara" , Category="Lainnya" },
        //         new SemuaMenuItem { Title="Transportasi", IconPath="/icons/car.png", Url="/transportasi", Category="Lainnya" },
        //         new SemuaMenuItem { Title="Bantuan", IconPath="/icons/help.png", Url="/bantuan", Category="Lainnya" },


        //     };
        // }


        public class SemuaMenuItem
        {
            public string Title { get; set; } = string.Empty;
            public string IconPath { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;

            public string Category { get; set; } = string.Empty;
        }
    }
}
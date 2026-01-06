using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper; // Sangat disarankan untuk mempermudah mapping data
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TestLandingPageNet8.Pages.UnitList
{
    public class UnitListModel : PageModel
    {
        public List<UnitItemList> UnitItems { get; set; } = new();

         public async Task OnGetAsync()
        {
            // Data Dummy Profil
            // 1. Ambil UserId dari Claims (User yang sedang login)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int userId))
            {
                Response.Redirect("/Login");
                return;
            }

            if (string.IsNullOrEmpty(userIdStr))
            {
                Response.Redirect("/Login");
                return;
            }

            // 2. Gunakan function Db.Connect() Anda
            using (var connection = Db.Connect())
            {
                // Query mengambil data berdasarkan UserId
                string sql = "SELECT * FROM V_ListKavlingUserPOrtal WHERE UserId = @Id";
                // Menggunakan Dapper untuk mapping otomatis ke class PortalUser
                UnitItems = (await connection.QueryAsync<UnitItemList>(sql, new { Id = userId })).ToList();

            }


        }

        // private void LoadUnitItems()
        // {
        //     UnitItems = new List<UnitItemList>
        // {
        //     new UnitItemList { UnitCode = "B-KAV001", Building = "Area Kawasan Industri Cileles", ImagePath = "/Image/image1.png", Status = "Penyewa", TicketCount = 5 },
        //     new UnitItemList { UnitCode = "B-KAV002", Building = "Area Kawasan Industri Cileles B", ImagePath = "/Image/image2.png", Status = "Pemilik", TicketCount = 2 },
        //     new UnitItemList { UnitCode = "B-KAV003", Building = "Area Kawasan Industri Cileles C", ImagePath = "/Image/image3.png", Status = "Pemilik", TicketCount = 1 },
        //     new UnitItemList { UnitCode = "B-KAV004", Building = "Area Kawasan Industri Cileles D", ImagePath = "/Image/image4.png", Status = "Penyewa", TicketCount = 0 }
        // };
        // }
    }

    public class UnitItemList
    {
        public string KavlingId { get; set; } = string.Empty;
        public string KavlingCode { get; set; } = string.Empty;
        public string kawasan { get; set; } = string.Empty;
        public string ImagePath { get; set; } = "/Image/image9.jpg";
        public string OwnerType { get; set; } = string.Empty;
        public string TicketKavlingCount { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class semuaTerkiniModel : PageModel
{
    public List<semuaTerkiniItem> SemuaTerkiniItems { get; set; } = new();
    public void OnGet()
    {
        semuaTerkiniList();

        
    }
// FILE: SemuaTerkini.cshtml.cs (Perubahan pada OnGet)

// public IActionResult OnGet(int page = 1)
// {
//     semuaTerkiniList();

//     int pageSize = 3; // 🔥 UBAH ke 3 agar sinkron dengan grid HTML
//     var skip = (page - 1) * pageSize;
    
//     var data = SemuaTerkiniItems
//                 .OrderBy(item => item.Id) // 🔥 TAMBAHKAN: OrderBy untuk stabilitas
//                 .Skip(skip)
//                 .Take(pageSize)
//                 .ToList();

//     if (!data.Any())
//         return new EmptyResult(); // Ini akan menghentikan loop setelah page 4

//     if (Request.Headers.ContainsKey("HX-Request"))
//         return Partial("ItemTerkiniList", data);

//     SemuaTerkiniItems = new List<semuaTerkiniItem>();
//     return Page();
// }



    private void semuaTerkiniList()
        {
            SemuaTerkiniItems = new List<semuaTerkiniItem>
            {
                new semuaTerkiniItem { Id = 1, Title="Perbaikan Jalan", CreateDate="30 Nov 2025", Price="Rp 250,000,000", Tag="Featured", ImageUrl="/Image/Image14.jpg", Description = "melibatkan serangkaian kegiatan terencana untuk mengembalikan, memperbaiki, atau meningkatkan kondisi fungsional jalan. Tujuannya adalah untuk mempertahankan kondisi jalan agar tetap optimal, aman, dan nyaman bagi pengguna, serta memperlancar mobilitas dan distribusi barang/jasa" },
                new semuaTerkiniItem { Id = 2, Title="Pembaruan System", CreateDate="30 Nov 2025", Price="Rp 180,000,000", Tag="Hot", ImageUrl="/Image/Image16.jpg", Description="Experience the epitome of urban living in this luxury loft situated in Bandung, featuring contemporary design and top-notch facilities." },
                new semuaTerkiniItem { Id = 3, Title="Fasilitas EV Charging", CreateDate="30 Nov 2025", Price="Rp 350,000,000", Tag="Premium", ImageUrl="/Image/Image15.png", Description="Discover elegance and comfort in this exquisite townhouse located in Surabaya, offering spacious interiors and modern conveniences." },
                new semuaTerkiniItem { Id = 4, Title="Pembangunan Mushola", CreateDate="30 Nov 2025", Price="Rp 270,000,000", Tag="Featured", ImageUrl="/Image/Image17.png", Description="Another modern villa with great environment and excellent facilities." },
                new semuaTerkiniItem { Id = 5,  Title="Perencanaan Pembangunan Taman", CreateDate="30 Nov 2025", Price="Rp 185,000,000", Tag="Hot", ImageUrl="/Image/Image18.jpeg", Description="Luxury loft with a gorgeous view of the city and modern rooms." },
                new semuaTerkiniItem { Id = 6, Title="Penanaman Pohon", CreateDate="30 Nov 2025", Price="Rp 360,000,000", Tag="Premium", ImageUrl="/Image/Image20.jpg", Description="Townhouse designed with contemporary style and comfortable living." },
                new semuaTerkiniItem { Id = 7, Title="Perbaikan Jalan", CreateDate="30 Nov 2025", Price="Rp 250,000,000", Tag="Featured", ImageUrl="/Image/Image14.jpg", Description = "melibatkan serangkaian kegiatan terencana untuk mengembalikan, memperbaiki, atau meningkatkan kondisi fungsional jalan. Tujuannya adalah untuk mempertahankan kondisi jalan agar tetap optimal, aman, dan nyaman bagi pengguna, serta memperlancar mobilitas dan distribusi barang/jasa" },
                new semuaTerkiniItem { Id = 8, Title="Pembaruan System", CreateDate="30 Nov 2025", Price="Rp 180,000,000", Tag="Hot", ImageUrl="/Image/Image16.jpg", Description="Experience the epitome of urban living in this luxury loft situated in Bandung, featuring contemporary design and top-notch facilities." },
                new semuaTerkiniItem { Id = 9, Title="Fasilitas EV Charging", CreateDate="30 Nov 2025", Price="Rp 350,000,000", Tag="Premium", ImageUrl="/Image/Image15.png", Description="Discover elegance and comfort in this exquisite townhouse located in Surabaya, offering spacious interiors and modern conveniences." },
                new semuaTerkiniItem { Id = 10, Title="Pembangunan Mushola", CreateDate="30 Nov 2025", Price="Rp 270,000,000", Tag="Featured", ImageUrl="/Image/Image17.png", Description="Another modern villa with great environment and excellent facilities." },
                new semuaTerkiniItem { Id = 11,  Title="Perencanaan Pembangunan Taman", CreateDate="30 Nov 2025", Price="Rp 185,000,000", Tag="Hot", ImageUrl="/Image/Image18.jpeg", Description="Luxury loft with a gorgeous view of the city and modern rooms." },
                new semuaTerkiniItem { Id = 12, Title="Penanaman Pohon", CreateDate="30 Nov 2025", Price="Rp 360,000,000", Tag="Premium", ImageUrl="/Image/Image20.jpg", Description="Townhouse designed with contemporary style and comfortable living." }
             };
        }
    public class semuaTerkiniItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreateDate { get; set; } = string.Empty;
    }

   

    

}
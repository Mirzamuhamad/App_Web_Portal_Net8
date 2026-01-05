using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DetailTerkiniModel : PageModel
{
    public DetailItem? Item { get; set; }

    public IActionResult OnGet(int id)
    {
        // Dummy data
        var list = new List<DetailItem>
         {
                new DetailItem { Id = 1, Title="Perbaikan Jalan", CreateDate="30 Nov 2025", Price="Rp 250,000,000", Tag="Featured", ImageUrl="/Image/Image14.jpg", Description = "melibatkan serangkaian kegiatan terencana untuk mengembalikan, memperbaiki, atau meningkatkan kondisi fungsional jalan. Tujuannya adalah untuk mempertahankan kondisi jalan agar tetap optimal, aman, dan nyaman bagi pengguna, serta memperlancar mobilitas dan distribusi barang/jasa" },
                new DetailItem { Id = 2, Title="Pembaruan System", CreateDate="30 Nov 2025", Price="Rp 180,000,000", Tag="Hot", ImageUrl="/Image/Image16.jpg", Description="Experience the epitome of urban living in this luxury loft situated in Bandung, featuring contemporary design and top-notch facilities." },
                new DetailItem { Id = 3, Title="Fasilitas EV Charging", CreateDate="30 Nov 2025", Price="Rp 350,000,000", Tag="Premium", ImageUrl="/Image/Image15.png", Description="Discover elegance and comfort in this exquisite townhouse located in Surabaya, offering spacious interiors and modern conveniences." },
                new DetailItem { Id = 4, Title="Pembangunan Mushola", CreateDate="30 Nov 2025", Price="Rp 270,000,000", Tag="Featured", ImageUrl="/Image/Image17.png", Description="Another modern villa with great environment and excellent facilities." },
                new DetailItem { Id = 5,  Title="Perencanaan Pembangunan Taman", CreateDate="30 Nov 2025", Price="Rp 185,000,000", Tag="Hot", ImageUrl="/Image/Image18.jpeg", Description="Luxury loft with a gorgeous view of the city and modern rooms." },
                new DetailItem { Id = 6, Title="Penanaman Pohon", CreateDate="30 Nov 2025", Price="Rp 360,000,000", Tag="Premium", ImageUrl="/Image/Image20.jpg", Description="Townhouse designed with contemporary style and comfortable living." }
             };

        Item = list.FirstOrDefault(x => x.Id == id);

        if (Item == null)
            return RedirectToPage("/Index");

        return Page();
    }
}

public class DetailItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreateDate { get; set; } = string.Empty;
}

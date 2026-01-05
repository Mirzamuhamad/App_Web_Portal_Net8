using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DetailAcaraModel : PageModel
{
    public DetailItemAcara? DetailAcara { get; set; }

    public IActionResult OnGet(int id)
    {
        // Dummy data
        var list = new List<DetailItemAcara>
        {
            new DetailItemAcara { Id = 1, Title="Property Expo", Location="Cileles", Price="Rp 500.000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image1.jpg", Description="Nikmati malam penuh musik dengan band-band ternama.", LocationUrl="https://maps.app.goo.gl/xYz123AbCdEfGh" },
            new DetailItemAcara { Id = 2, Title="Indonesia Property Forum", Location="Tangerang Selatan", Price="Rp 150.000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image2.jpg", Description="Pameran seni dengan karya seniman lokal & internasional.", LocationUrl="https://maps.app.goo.gl/xYz123AbCdEfGh" },
            new DetailItemAcara { Id = 3, Title="Halloween Event", Location="BSD City", Price="Rp 200.000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image3.jpg", Description="Festival kuliner terbesar tahun ini.", LocationUrl="https://maps.app.goo.gl/xYz123AbCdEfGh" },
            new DetailItemAcara { Id = 4, Title="Festival Music", Location="Yogyakarta", Price="Rp 300.000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image4.webp", Description="Workshop intensif bersama fotografer profesional.", LocationUrl="https://maps.app.goo.gl/xYz123AbCdEfGh" }
        };

        DetailAcara = list.FirstOrDefault(x => x.Id == id);

        if (DetailAcara == null)
            return RedirectToPage("/Index");

        return Page();
    }
}

public class DetailItemAcara
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; //= string.Empty; untuk menghindari null reference jadi kalau tidak diisi tetap ada isinya
    public string Location { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationUrl { get; set; } = string.Empty;
}

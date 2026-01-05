using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class semuaAcaraModel : PageModel
{
    public List<semuaAcaraItem> SemuaAcaraItems { get; set; } = new();
    public void OnGet()
    {
        semuaAcaraList();
    }

    private void semuaAcaraList()
        {
            SemuaAcaraItems = new List<semuaAcaraItem>
            {
                
                new semuaAcaraItem { Id = 1, Title="Property Expo", Location="Cileles", Price="Rp 500,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image1.jpg", Description="Nikmati malam penuh musik dengan penampilan dari band-band ternama di konser spektakuler ini." },
                new semuaAcaraItem { Id = 2, Title="Indonesia Property Forum", Location="Tangerang Selatan", Price="Rp 150,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image2.jpg", Description="Jelajahi karya seni menakjubkan dari seniman lokal dan internasional di pameran seni ini." },
                new semuaAcaraItem { Id = 3, Title="Halloween", Location="BSD City", Price="Rp 200,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image3.jpg", Description="Cicipi berbagai hidangan lezat dari seluruh nusantara di festival kuliner terbesar tahun ini." },
                new semuaAcaraItem { Id = 4, Title="Festival Music", Location="Yogyakarta", Price="Rp 300,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image4.webp", Description="Tingkatkan keterampilan fotografi Anda dengan mengikuti workshop intensif bersama fotografer profesional." }
            
             };
        }
    public class semuaAcaraItem
    {
        public int Id { get; set; }
        public string Title { get; set; }= string.Empty;
        public string Location { get; set; }= string.Empty;
        public string Price { get; set; }= string.Empty;
        public string StartDate { get; set; }= string.Empty;
        public string EndDate { get; set; }= string.Empty;
        public string ImageUrl { get; set; }= string.Empty;
        public string Description { get; set; }= string.Empty;
    }

}
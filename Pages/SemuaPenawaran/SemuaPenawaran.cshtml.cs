using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class semuaPenawaranModel : PageModel
{
    public List<semuaPenawaranItem> SemuaPenawaranItems { get; set; } = new();
    public void OnGet()
    {
        semuaPenawaranList();
    }

    private void semuaPenawaranList()
        {
            SemuaPenawaranItems = new List<semuaPenawaranItem>
            {   
                new semuaPenawaranItem { Id = 1, Title="Gudang Tekstil", Location="Jakarta", Price="Rp 250,000,000", Tag="Featured", ImageUrl="/Image/Image10.jpg", Description="A beautiful modern villa located in the heart of Jakarta with stunning architecture and luxurious amenities." },
                new semuaPenawaranItem { Id = 2, Title="Kavling Kosong", Location="Cileles", Price="Rp 180,000,000", Tag="Hot", ImageUrl="/Image/Image12.jpg", Description="Experience the epitome of urban living in this luxury loft situated in Bandung, featuring contemporary design and top-notch facilities." },
                new semuaPenawaranItem { Id = 3, Title="Pabrik", Location="Cileles", Price="Rp 350,000,000", Tag="Premium", ImageUrl="/Image/Image11.jpg", Description="Discover elegance and comfort in this exquisite townhouse located in Surabaya, offering spacious interiors and modern conveniences." },
                new semuaPenawaranItem { Id = 4, Title="Gudang Makanan", Location="Jakarta", Price="Rp 270,000,000", Tag="Featured", ImageUrl="/Image/Image13.jpg", Description="Another modern villa with great environment and excellent facilities." },
                new semuaPenawaranItem { Id = 5, Title="Kavling kosong B", Location="Bandung", Price="Rp 185,000,000", Tag="Hot", ImageUrl="/Image/Image12.jpg", Description="Luxury loft with a gorgeous view of the city and modern rooms." },
                new semuaPenawaranItem { Id = 6, Title="Gudang Barang", Location="Surabaya", Price="Rp 360,000,000", Tag="Premium", ImageUrl="/Image/Image10.jpg", Description="Townhouse designed with contemporary style and comfortable living." }

               };
        }
        public class semuaPenawaranItem
    {
        public int Id { get; set; }
        public string Title { get; set; }= string.Empty;
        public string Location { get; set; }= string.Empty;
        public string Price { get; set; }= string.Empty;
        public string Tag { get; set; }= string.Empty;
        public string ImageUrl { get; set; }= string.Empty;
        public string Description { get; set; }= string.Empty;
    }

}
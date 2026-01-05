using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DetailPenawaranModel : PageModel
{
    public DetailItemPenawaran? Item { get; set; }

    public IActionResult OnGet(int id)
    {
        // Dummy data
        var list = new List<DetailItemPenawaran>
         {
                new DetailItemPenawaran {
                    Id = 1, Title="Gudang Tekstil", Location="Jakarta", Price="Rp 250,000,000", Tag="Featured",
                    ImageUrl="/Image/Image10.jpg",
                    Description="Penawaran spesial untuk Gudang Tekstil di Jakarta! Dapatkan ruang penyimpanan luas yang ideal untuk industri garmen dan konveksi. Promo terbatas: diskon biaya sewa tahunan dan bonus perawatan fasilitas selama 3 bulan pertama."
                },

                new DetailItemPenawaran {
                    Id = 2, Title="Kavling Kosong", Location="Cileles", Price="Rp 180,000,000", Tag="Hot",
                    ImageUrl="/Image/Image12.jpg",
                    Description="Kavling kosong siap bangun di kawasan Cileles dengan harga terbaik. Cocok untuk investasi jangka panjang maupun pembangunan hunian. Promo bulan ini: gratis biaya pengurusan izin awal dan potongan harga khusus untuk pembelian tunai."
                },

                new DetailItemPenawaran {
                    Id = 3, Title="Pabrik", Location="Cileles", Price="Rp 350,000,000", Tag="Premium",
                    ImageUrl="/Image/Image11.jpg",
                    Description="Penawaran menarik untuk pabrik siap pakai di Cileles! Dilengkapi area produksi luas dan akses kendaraan besar. Dapatkan cashback hingga 10% serta fasilitas konsultasi layout produksi secara gratis."
                },

                new DetailItemPenawaran {
                    Id = 4, Title="Gudang Makanan", Location="Jakarta", Price="Rp 270,000,000", Tag="Featured",
                    ImageUrl="/Image/Image13.jpg",
                    Description="Gudang makanan strategis di Jakarta, cocok untuk distribusi dan penyimpanan produk F&B. Free instalasi rak awal dan diskon biaya keamanan selama 6 bulan. Penawaran terbatas!"
                },

                new DetailItemPenawaran {
                    Id = 5, Title="Kavling kosong B", Location="Bandung", Price="Rp 185,000,000", Tag="Hot",
                    ImageUrl="/Image/Image12.jpg",
                    Description="Kavling kosong B di Bandung dengan lokasi premium dan nilai investasi tinggi. Promo khusus bulan ini: cicilan tanpa bunga hingga 12 bulan dan gratis biaya notaris."
                },

                new DetailItemPenawaran {
                    Id = 6, Title="Gudang Barang", Location="Surabaya", Price="Rp 360,000,000", Tag="Premium",
                    ImageUrl="/Image/Image10.jpg",
                    Description="Gudang barang di Surabaya dengan akses logistik terbaik. Cocok untuk penyimpanan retail maupun distribusi besar. Nikmati potongan harga sewa dan gratis biaya perawatan selama 2 bulan."
                },

             };

        Item = list.FirstOrDefault(x => x.Id == id);

        if (Item == null)
            return RedirectToPage("/Index");

        return Page();
    }
}

public class DetailItemPenawaran
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

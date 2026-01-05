using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using Dapper;


namespace TestLandingPageNet8.Pages
{
    public class IndexModel : PageModel
    {
        // private readonly IDbConnectionFactory _db; V1 dengan dependency injection

        // public IndexModel(IDbConnectionFactory db) v1 dengan dependency injection
        // {
        //     _db = db;
        // }
        public List<LoadDummyTerkini> Terkini { get; set; } = new();
        public List<MsMenuItem> ItemsMenu { get; set; } = new(); // contoh list menu dari database buat menu list
        public List<PropertyItem> Properties { get; set; } = new(); // list properti buat carousel membuat data 
        public List<ItemCaraouselAcara> Acara { get; set; } = new(); // list acara buat carousel membuat data
        public List<HomeMenuItem> HomeMenuItems { get; set; } = new();

        public void OnGet()
        {
            LoadDummyTerkini();
            LoadItemsFromDatabase(); // contoh ambil data dari database 
            LoadDummyProperties();   // nanti ganti SQL juga
            LoadDummyAcara();
            LoadItemsMenu();
        }

        private void LoadItemsFromDatabase() // contoh ambil data dari database loadnya
        {
            try
            {
                // using var conn = _db.CreateConnection();
                // conn.Open();
                using var conn = Db.Connect();
                conn.Open();

                using var cmd = new SqlCommand("SELECT TOP 10 * FROM MsMenu", conn);
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ItemsMenu.Add(new MsMenuItem
                    {
                        MenuId = dr["MenuId"].ToString(),
                        MenuName = dr["MenuName"].ToString(),
                        MenuUrl = dr["MenuUrl"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                // log error atau tampilkan di console
                Console.WriteLine("Error loading menu items: " + ex.Message);
            }
        }

        private void LoadDummyTerkini()
        {
            Terkini = new List<LoadDummyTerkini>
            {
                new LoadDummyTerkini { Id = 1, Title="Perbaikan Jalan", CreateDate="30 Nov 2025", Price="Rp 250,000,000", Tag="Featured", ImageUrl="/Image/Image14.jpg", Description = "melibatkan serangkaian kegiatan terencana untuk mengembalikan, memperbaiki, atau meningkatkan kondisi fungsional jalan. Tujuannya adalah untuk mempertahankan kondisi jalan agar tetap optimal, aman, dan nyaman bagi pengguna, serta memperlancar mobilitas dan distribusi barang/jasa" },
                new LoadDummyTerkini { Id = 2, Title="Pembaruan System", CreateDate="30 Nov 2025", Price="Rp 180,000,000", Tag="Hot", ImageUrl="/Image/Image16.jpg", Description="Experience the epitome of urban living in this luxury loft situated in Bandung, featuring contemporary design and top-notch facilities." },
                new LoadDummyTerkini { Id = 3, Title="Fasilitas EV Charging", CreateDate="30 Nov 2025", Price="Rp 350,000,000", Tag="Premium", ImageUrl="/Image/Image15.png", Description="Discover elegance and comfort in this exquisite townhouse located in Surabaya, offering spacious interiors and modern conveniences." },
                new LoadDummyTerkini { Id = 4, Title="Pembangunan Mushola", CreateDate="30 Nov 2025", Price="Rp 270,000,000", Tag="Featured", ImageUrl="/Image/Image17.png", Description="Another modern villa with great environment and excellent facilities." },
                new LoadDummyTerkini { Id = 5,  Title="Perencanaan Pembangunan Taman", CreateDate="30 Nov 2025", Price="Rp 185,000,000", Tag="Hot", ImageUrl="/Image/Image18.jpeg", Description="Luxury loft with a gorgeous view of the city and modern rooms." },
                new LoadDummyTerkini { Id = 6, Title="Penanaman Pohon", CreateDate="30 Nov 2025", Price="Rp 360,000,000", Tag="Premium", ImageUrl="/Image/Image20.jpg", Description="Townhouse designed with contemporary style and comfortable living." }
             };
        }

        private void LoadDummyProperties()
        {
            Properties = new List<PropertyItem>
            {
                new PropertyItem { Id = 1, Title="Gudang Tekstil", Location="Jakarta", Price="Rp 250,000,000", Tag="Featured", ImageUrl="/Image/Image10.jpg", Description="A beautiful modern villa located in the heart of Jakarta with stunning architecture and luxurious amenities." },
                new PropertyItem { Id = 2, Title="Kavling Kosong", Location="Cileles", Price="Rp 180,000,000", Tag="Hot", ImageUrl="/Image/Image12.jpg", Description="Experience the epitome of urban living in this luxury loft situated in Bandung, featuring contemporary design and top-notch facilities." },
                new PropertyItem { Id = 3, Title="Pabrik", Location="Cileles", Price="Rp 350,000,000", Tag="Premium", ImageUrl="/Image/Image11.jpg", Description="Discover elegance and comfort in this exquisite townhouse located in Surabaya, offering spacious interiors and modern conveniences." },
                new PropertyItem { Id = 4, Title="Gudang Makanan", Location="Jakarta", Price="Rp 270,000,000", Tag="Featured", ImageUrl="/Image/Image13.jpg", Description="Another modern villa with great environment and excellent facilities." },
                new PropertyItem { Id = 5, Title="Kavling kosong B", Location="Bandung", Price="Rp 185,000,000", Tag="Hot", ImageUrl="/Image/Image12.jpg", Description="Luxury loft with a gorgeous view of the city and modern rooms." },
                new PropertyItem { Id = 6, Title="Gudang Barang", Location="Surabaya", Price="Rp 360,000,000", Tag="Premium", ImageUrl="/Image/Image10.jpg", Description="Townhouse designed with contemporary style and comfortable living." }

               };
        }


        private void LoadDummyAcara()
        {
            // Implementasi pengambilan data acara dari database atau sumber lainnya
            Acara = new List<ItemCaraouselAcara>
            {
                new ItemCaraouselAcara { Id = 1, Title="Property Expo", Location="Cileles", Price="Rp 500,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image1.jpg", Description="Nikmati malam penuh musik dengan penampilan dari band-band ternama di konser spektakuler ini." },
                new ItemCaraouselAcara { Id = 2, Title="Indonesia Property Forum", Location="Tangerang Selatan", Price="Rp 150,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image2.jpg", Description="Jelajahi karya seni menakjubkan dari seniman lokal dan internasional di pameran seni ini." },
                new ItemCaraouselAcara { Id = 3, Title="Halloween", Location="BSD City", Price="Rp 200,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image3.jpg", Description="Cicipi berbagai hidangan lezat dari seluruh nusantara di festival kuliner terbesar tahun ini." },
                new ItemCaraouselAcara { Id = 4, Title="Festival Music", Location="Yogyakarta", Price="Rp 300,000", StartDate="10 Nov 2025", EndDate="13 Nov 2025", ImageUrl="/Acara/Image4.webp", Description="Tingkatkan keterampilan fotografi Anda dengan mengikuti workshop intensif bersama fotografer profesional." }
            };
        }

        // class ambil data menu dari database
        private void LoadItemsMenu()
        {
            using (var conn = Db.Connect())
            {
                string sql = @"
            SELECT
                Title,
                IconPath,
                Url
            FROM V_AppMenuPortal WHERE FgIsHome = 'Y'
        ";

                HomeMenuItems = conn.Query<HomeMenuItem>(sql).ToList();
            }
        }

        // class ambil data menu dari hardcode LIst
        //     private void LoadItemsMenu()
        //     {
        //         HomeMenuItems = new List<HomeMenuItem>
        //         {
        //         new HomeMenuItem { Title="Unit Saya", IconPath ="/icons/home.png", Url="/UnitList/UnitList" },
        //         new HomeMenuItem { Title="Service Order", IconPath ="/icons/mechanic.png", Url="/darurat" },
        //         new HomeMenuItem { Title="For Sale or Lease", IconPath ="/icons/house.png", Url="/acara" },
        //         new HomeMenuItem { Title="Tagihan", IconPath ="/icons/bill.png", Url="/transportasi" },

        //         // new HomeMenuItem { Title="Izin & Upload", IconPath ="/icons/hand-paper-check.png", Url="/unit" },
        //         new HomeMenuItem { Title="Complaint", IconPath ="/icons/complaint.png", Url="/CreateComplaint/CreateComplaint" },
        //         new HomeMenuItem { Title="Pelanggaran", IconPath ="/icons/handcuffs.png", Url="/cctv" },
        //         new HomeMenuItem { Title="Asset Booking", IconPath ="/icons/asset.png", Url="/semua" },

        //         new HomeMenuItem { Title="FAQ", IconPath ="/icons/faq.png", Url="/properti" },
        //         new HomeMenuItem { Title="Darurat", IconPath ="/icons/sos.png", Url="/darurat" },
        //         new HomeMenuItem { Title="Acara", IconPath ="/icons/diagram.png", Url="/acara" },
        //         new HomeMenuItem { Title="Transportasi", IconPath ="/icons/car.png", Url="/transportasi" },
        //         // new HomeMenuItem { Title="Information Center", IconPath ="/icons/info.png", Url="/unit" },
        //         // new HomeMenuItem { Title="Bantuan", IconPath ="/icons/help.png", Url="/bantuan" },
        //         // new HomeMenuItem { Title="CCTV", IconPath ="/icons/cctv.png", Url="/cctv" },
        //         new HomeMenuItem { Title="Lihat Semua", IconPath ="/icons/more.png", Url="/SemuaMenu/SemuaMenu" }


        // };
        //     }


    }

    public class MsMenuItem
    {
        public string MenuId { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;
        public string MenuUrl { get; set; } = string.Empty;
    }

    public class LoadDummyTerkini
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreateDate { get; set; } = string.Empty;
    }

    public class PropertyItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ItemCaraouselAcara
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class HomeMenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

public class semuaPenawaranModel : PageModel
{
    public const int PageSize = 9;

    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    public List<semuaPenawaranItem> SemuaPenawaranItems { get; set; } = new();

    public void OnGet()
    {
        SemuaPenawaranItems = LoadItems(null, Search);
    }

    public IActionResult OnGetItems(int? lastId, string search = "")
    {
        var items = LoadItems(lastId, search);

        ViewData["Search"] = search ?? string.Empty;

        return Partial("ItemPenawaranList", items);
    }

    private static List<semuaPenawaranItem> LoadItems(int? lastId, string? search)
    {
        var items = new List<semuaPenawaranItem>();
        var keyword = search?.Trim() ?? string.Empty;

        try
        {
            using var conn = Db.Connect();
            conn.Open();

            const string sql = @"
                SELECT
                    Id,
                    Title,
                    CreatedDate,
                    Tag,
                    ImageUrl,
                    Description,
                    Price,
                    Location
                FROM V_SectionPromo
                WHERE
                    (@LastId IS NULL OR Id < @LastId)
                    AND (
                        @Search = ''
                        OR Title LIKE @LikeSearch
                        OR Description LIKE @LikeSearch
                        OR Tag LIKE @LikeSearch
                        OR Location LIKE @LikeSearch
                    )
                ORDER BY Id DESC
                OFFSET 0 ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LastId", lastId.HasValue ? lastId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", keyword);
            cmd.Parameters.AddWithValue("@LikeSearch", $"%{keyword}%");
            cmd.Parameters.AddWithValue("@PageSize", PageSize);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                items.Add(new semuaPenawaranItem
                {
                    Id = Convert.ToInt32(dr["Id"]),
                    Title = ReadString(dr, "Title"),
                    Location = ReadString(dr, "Location"),
                    Price = FormatPrice(dr["Price"]),
                    Tag = ReadString(dr, "Tag"),
                    ImageUrl = ReadImageUrl(dr, "ImageUrl", "/Image/default.jpg"),
                    Description = ReadString(dr, "Description"),
                    CreateDate = FormatDate(dr["CreatedDate"])
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading semua penawaran items: " + ex.Message);
        }

        return items;
    }

    private static string ReadString(SqlDataReader dr, string columnName)
    {
        return dr[columnName] != DBNull.Value ? dr[columnName]?.ToString() ?? string.Empty : string.Empty;
    }

    private static string ReadImageUrl(SqlDataReader dr, string columnName, string fallback)
    {
        var imageUrl = ReadString(dr, columnName);
        return string.IsNullOrWhiteSpace(imageUrl) ? fallback : imageUrl;
    }

    private static string FormatDate(object value)
    {
        return value != DBNull.Value && DateTime.TryParse(value.ToString(), out var date)
            ? date.ToString("dd MMM yyyy")
            : string.Empty;
    }

    private static string FormatPrice(object value)
    {
        if (value == DBNull.Value)
        {
            return string.Empty;
        }

        return decimal.TryParse(value.ToString(), out var price)
            ? $"Rp {price:N0}"
            : value.ToString() ?? string.Empty;
    }

    public class semuaPenawaranItem
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
}

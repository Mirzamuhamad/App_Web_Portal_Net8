using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

public class semuaTerkiniModel : PageModel
{
    public const int PageSize = 9;

    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    public List<semuaTerkiniItem> SemuaTerkiniItems { get; set; } = new();

    public void OnGet()
    {
        SemuaTerkiniItems = LoadItems(null, Search);
    }

    public IActionResult OnGetItems(int? lastId, string search = "")
    {
        var items = LoadItems(lastId, search);

        ViewData["Search"] = search ?? string.Empty;

        return Partial("ItemTerkiniList", items);
    }

    private static List<semuaTerkiniItem> LoadItems(int? lastId, string? search)
    {
        var items = new List<semuaTerkiniItem>();
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
                    Price
                FROM V_SectionInfo
                WHERE
                    (@LastId IS NULL OR Id < @LastId)
                    AND (
                        @Search = ''
                        OR Title LIKE @LikeSearch
                        OR Description LIKE @LikeSearch
                        OR Tag LIKE @LikeSearch
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
                items.Add(new semuaTerkiniItem
                {
                    Id = Convert.ToInt32(dr["Id"]),
                    Title = ReadString(dr, "Title"),
                    CreateDate = FormatDate(dr["CreatedDate"]),
                    Price = FormatPrice(dr["Price"]),
                    Tag = ReadString(dr, "Tag"),
                    ImageUrl = ReadImageUrl(dr, "ImageUrl", "/Image/default.jpg"),
                    Description = ReadString(dr, "Description")
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading semua terkini items: " + ex.Message);
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

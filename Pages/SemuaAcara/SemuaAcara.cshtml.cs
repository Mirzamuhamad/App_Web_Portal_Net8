using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

public class semuaAcaraModel : PageModel
{
    public const int PageSize = 9;

    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    public List<semuaAcaraItem> SemuaAcaraItems { get; set; } = new();

    public void OnGet()
    {
        SemuaAcaraItems = LoadItems(null, Search);
    }

    public IActionResult OnGetItems(int? lastId, string search = "")
    {
        var items = LoadItems(lastId, search);

        ViewData["Search"] = search ?? string.Empty;

        return Partial("ItemAcaraList", items);
    }

    private static List<semuaAcaraItem> LoadItems(int? lastId, string? search)
    {
        var items = new List<semuaAcaraItem>();
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
                    ImageUrl,
                    Description,
                    StartDate,
                    EndDate,
                    Location,
                    Price
                FROM V_SectionEvent
                WHERE
                    (@LastId IS NULL OR Id < @LastId)
                    AND (
                        @Search = ''
                        OR Title LIKE @LikeSearch
                        OR Description LIKE @LikeSearch
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
                items.Add(new semuaAcaraItem
                {
                    Id = Convert.ToInt32(dr["Id"]),
                    Title = ReadString(dr, "Title"),
                    Location = ReadString(dr, "Location"),
                    Price = FormatPrice(dr["Price"]),
                    CreatedDate = FormatDate(dr["CreatedDate"]),
                    StartDate = FormatDate(dr["StartDate"]),
                    EndDate = FormatDate(dr["EndDate"]),
                    ImageUrl = ReadImageUrl(dr, "ImageUrl", "/Acara/default.jpg"),
                    Description = ReadString(dr, "Description")
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading semua acara items: " + ex.Message);
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

    public class semuaAcaraItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

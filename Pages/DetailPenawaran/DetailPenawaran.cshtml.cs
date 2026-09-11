using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

public class DetailPenawaranModel : PageModel
{
    public DetailItemPenawaran? Item { get; set; }
    public List<DetailPenawaranPhoto> Photos { get; set; } = new();

    public IActionResult OnGet(long id)
    {
        try
        {
            using var conn = Db.Connect();
            conn.Open();

            {
                using var cmd = new SqlCommand("SELECT * FROM V_SectionPromo WHERE Id = @Id", conn);
                cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

                using var dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    Item = new DetailItemPenawaran
                    {
                        Id = Convert.ToInt64(dr["Id"]),
                        Title = dr["Title"] != DBNull.Value ? dr["Title"].ToString() ?? string.Empty : string.Empty,
                        Location = dr["Location"] != DBNull.Value ? dr["Location"].ToString() ?? string.Empty : string.Empty,
                        Price = dr["Price"] != DBNull.Value && decimal.TryParse(dr["Price"].ToString(), out var priceVal)
                            ? $"Rp {priceVal:N0}"
                            : (dr["Price"] != DBNull.Value ? dr["Price"].ToString() ?? string.Empty : string.Empty),
                        Tag = dr["Tag"] != DBNull.Value ? dr["Tag"].ToString() ?? string.Empty : string.Empty,
                        ImageUrl = dr["ImageUrl"] != DBNull.Value && !string.IsNullOrWhiteSpace(dr["ImageUrl"].ToString())
                            ? dr["ImageUrl"].ToString()!
                            : "/Image/default.jpg",
                        Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() ?? string.Empty : string.Empty,
                        CreateDate = dr["CreatedDate"] != DBNull.Value && DateTime.TryParse(dr["CreatedDate"].ToString(), out var createDate)
                            ? createDate.ToString("dd MMM yyyy")
                            : string.Empty
                    };
                }
            }

            if (Item != null)
            {
                const string photoSql = @"
                    SELECT PhotoID, dbo.Dashboard_Url(ImagePath) AS ImagePath, SortOrder
                    FROM dbo.tbl_WebContentPromoPhoto
                    WHERE ContentID = @ContentID
                    ORDER BY SortOrder, PhotoID";

                using var photoCmd = new SqlCommand(photoSql, conn);
                photoCmd.Parameters.Add("@ContentID", SqlDbType.BigInt).Value = Item.Id;

                using var photoReader = photoCmd.ExecuteReader();
                while (photoReader.Read())
                {
                    var imagePath = photoReader["ImagePath"]?.ToString();
                    if (string.IsNullOrWhiteSpace(imagePath))
                    {
                        continue;
                    }

                    Photos.Add(new DetailPenawaranPhoto
                    {
                        PhotoId = Convert.ToInt64(photoReader["PhotoID"]),
                        ImagePath = imagePath,
                        SortOrder = Convert.ToInt32(photoReader["SortOrder"])
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading penawaran detail: " + ex.Message);
        }

        if (Item == null)
            return RedirectToPage("/Index");

        return Page();
    }
}

public class DetailItemPenawaran
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreateDate { get; set; } = string.Empty;
}

public class DetailPenawaranPhoto
{
    public long PhotoId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

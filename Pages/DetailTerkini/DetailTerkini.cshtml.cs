using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class DetailTerkiniModel : PageModel
{
    public DetailItem? Item { get; set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            using var conn = Db.Connect();
            conn.Open();

            using var cmd = new SqlCommand("SELECT * FROM V_SectionInfo WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                Item = new DetailItem
                {
                    Id = Convert.ToInt32(dr["Id"]),
                    Title = dr["Title"] != DBNull.Value ? dr["Title"].ToString() : string.Empty,
                    Price = dr["Price"] != DBNull.Value && decimal.TryParse(dr["Price"].ToString(), out var priceVal) 
                        ? $"Rp {priceVal:N0}" 
                        : (dr["Price"] != DBNull.Value ? dr["Price"].ToString() : string.Empty),
                    Tag = dr["Tag"] != DBNull.Value ? dr["Tag"].ToString() : string.Empty,
                    ImageUrl = dr["ImageUrl"] != DBNull.Value ? dr["ImageUrl"].ToString() : "/Image/default.jpg",
                    Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : string.Empty,
                    CreateDate = dr["CreatedDate"] != DBNull.Value && DateTime.TryParse(dr["CreatedDate"].ToString(), out var createDate)
                        ? createDate.ToString("dd MMM yyyy")
                        : string.Empty
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading terkini detail: " + ex.Message);
        }

        if (Item == null)
            return RedirectToPage("/Index");

        return Page();
    }
}

public class DetailItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreateDate { get; set; } = string.Empty;
}
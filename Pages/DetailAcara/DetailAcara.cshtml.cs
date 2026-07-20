using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

public class DetailAcaraModel : PageModel
{
    public DetailItemAcara? DetailAcara { get; set; }

    public IActionResult OnGet(int id)
    {
        var list = new List<DetailItemAcara>();

        try
        {
            using var conn = Db.Connect();
            conn.Open();

            // Gunakan parameter untuk mencegah SQL Injection dan ambil data berdasarkan ID jika diperlukan,
            // atau ambil data dari V_SectionEvent sesuai dengan konteks Acara
            using var cmd = new SqlCommand("SELECT * FROM V_SectionEvent WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            
            using var dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                DetailAcara = new DetailItemAcara
                {
                    Id = Convert.ToInt32(dr["Id"]),
                    Title = dr["Title"] != DBNull.Value ? dr["Title"].ToString() : string.Empty,
                    Location = dr["Location"] != DBNull.Value ? dr["Location"].ToString() : string.Empty,
                    Price = dr["Price"] != DBNull.Value && decimal.TryParse(dr["Price"].ToString(), out var priceVal) 
                        ? $"Rp {priceVal:N0}" 
                        : (dr["Price"] != DBNull.Value ? dr["Price"].ToString() : string.Empty),
                    StartDate = dr["StartDate"] != DBNull.Value && DateTime.TryParse(dr["StartDate"].ToString(), out var startDate)
                        ? startDate.ToString("dd MMM yyyy")
                        : string.Empty,
                    EndDate = dr["EndDate"] != DBNull.Value && DateTime.TryParse(dr["EndDate"].ToString(), out var endDate)
                        ? endDate.ToString("dd MMM yyyy")
                        : string.Empty,
                    ImageUrl = dr["ImageUrl"] != DBNull.Value ? dr["ImageUrl"].ToString() : "/Acara/default.jpg",
                    Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : string.Empty
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading event detail: " + ex.Message);
        }

        if (DetailAcara == null)
            return RedirectToPage("/Index");

        return Page();
    }
}

public class DetailItemAcara
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationUrl { get; set; } = string.Empty;
}
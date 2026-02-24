
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization; // Tambahkan ini


[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly IConfiguration _config;

    public ResetPasswordModel(IConfiguration config)
    {
        _config = config;
    }

    

    [BindProperty] public string Token { get; set; }
    
    [BindProperty, Required, MinLength(6, ErrorMessage = "Password minimal 6 karakter")]
    public string NewPassword { get; set; }

    [BindProperty, Required, Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak cocok")]
    public string ConfirmPassword { get; set; }

    // Saat halaman dibuka melalui link email
    public IActionResult OnGet(string token)
    {
        if (string.IsNullOrEmpty(token)) return RedirectToPage("/Login");
        
        Token = token;
        return Page();
    }

    // Saat form disubmit
    public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid)
    {
        return new JsonResult(new { 
            success = false, 
            message = "Pastikan semua data terisi dengan benar.",
            errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            )
        });
    }

    if (NewPassword != ConfirmPassword)
    {
        return new JsonResult(new { success = false, message = "Konfirmasi password tidak cocok." });
    }

    try 
    {
        using var conn = Db.Connect();
        await conn.OpenAsync();

        // 1. Validasi Token
        using var cmdCheck = new SqlCommand("SELECT UserId FROM PortalUsers WHERE ResetToken = @Token", conn);
        cmdCheck.Parameters.AddWithValue("@Token", Token);
        var userId = await cmdCheck.ExecuteScalarAsync();

        if (userId == null)
        {
            return new JsonResult(new { success = false, message = "Tautan tidak valid atau kedaluwarsa." });
        }

        // 2. Hash & Update
        string hashedPassword = HashPassword(NewPassword);
        using var cmdUpdate = new SqlCommand(@"
            UPDATE PortalUsers 
            SET PasswordHash = @Pwd, ResetToken = NULL 
            WHERE UserId = @Id", conn);
            
        cmdUpdate.Parameters.AddWithValue("@Pwd", hashedPassword);
        cmdUpdate.Parameters.AddWithValue("@Id", userId);
        await cmdUpdate.ExecuteNonQueryAsync();

        return new JsonResult(new { success = true, message = "Password berhasil diubah! Mengalihkan ke halaman login..." });
    }
    catch (Exception ex)
    {
        return new JsonResult(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
    }
}
    // Fungsi Hash yang konsisten dengan sistem Anda
    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

}
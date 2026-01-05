using Microsoft.AspNetCore.Http;

public class RegisterInput
{
    public string UserType { get; set; } // OWNER / COMPANY

    // COMMON
    public string Email { get; set; }
    public string Password { get; set; }
    public string RetypePassword { get; set; }

    // OWNER
    public string OwnerName { get; set; }
    public bool OwnerHasKavling { get; set; }
    public string OwnerKavlingDesc { get; set; }
    public IFormFile OwnerDocument { get; set; }

    // COMPANY
    public string CompanyName { get; set; }
    public bool CompanyHasKavling { get; set; }
    public string CompanyKavlingDesc { get; set; }
    public IFormFile CompanyDocument { get; set; }
}

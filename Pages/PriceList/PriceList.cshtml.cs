using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestLandingPageNet8.Pages.PriceList;

public class PriceListModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PriceListModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    // Tambahkan properti untuk menangkap parameter TypeService dari URL (GET)
    [BindProperty(SupportsGet = true)]
    public string TypeService { get; set; } = string.Empty;

    public List<PermitPriceItem> PermitPrices { get; private set; } = new();

    public string? LoadError { get; private set; }

    public async Task OnGetAsync()
    {
        Search = Search?.Trim() ?? string.Empty;
        TypeService = TypeService?.Trim() ?? string.Empty;

        const string sql = @"
            SELECT
                ContentID,
                PermitID,
                Title,
                SubTitle,
                [Description],
                PermitPrice AS Price,
                TypeService,
                DocumentPath,
                dbo.Dashboard_Url(REPLACE(DocumentPath, '~/','')) AS DocumentUrl,
                OriginalFileName,
                UploadedDate
            FROM dbo.V_WebContentCMS
            WHERE ContentType = 'PERMIT'
              AND [Status] = 1
              AND PermitID IS NOT NULL
              AND (@TypeService = '' OR TypeService = @TypeService)
              AND (
                    @Search = ''
                    OR Title LIKE @LikeSearch
                    OR ISNULL(SubTitle, '') LIKE @LikeSearch
                    OR ISNULL([Description], '') LIKE @LikeSearch
                    OR ISNULL(TypeService, '') LIKE @LikeSearch
                  )
            ORDER BY SortOrder ASC, ContentID DESC;";

        try
        {
            using var connection = Db.Connect();
            await connection.OpenAsync();

            var result = await connection.QueryAsync<PermitPriceItem>(sql, new
            {
                Search,
                LikeSearch = $"%{Search}%",
                TypeService
            });

            PermitPrices = result.ToList();
        }
        catch (Exception ex)
        {
            LoadError = "Daftar harga perizinan belum dapat dimuat. Silakan coba kembali.";
            Console.Error.WriteLine($"Gagal memuat pricelist perizinan: {ex}");
        }
    }
    

    public async Task<IActionResult> OnGetDownloadAsync(long contentId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                dbo.Dashboard_Url(REPLACE(DocumentPath, '~/','')) AS DocumentUrl,
                OriginalFileName
            FROM dbo.V_WebContentCMS
            WHERE ContentID = @ContentID
              AND ContentType = 'PERMIT'
              AND [Status] = 1
              AND PermitID IS NOT NULL;";

        DownloadDocument? document;

        using (var connection = Db.Connect())
        {
            await connection.OpenAsync(cancellationToken);
            document = await connection.QuerySingleOrDefaultAsync<DownloadDocument>(sql, new
            {
                ContentID = contentId
            });
        }

        if (document is null || string.IsNullOrWhiteSpace(document.DocumentUrl))
        {
            return NotFound("Dokumen tidak tersedia.");
        }

        if (!Uri.TryCreate(document.DocumentUrl, UriKind.Absolute, out var documentUri))
        {
            var applicationUri = new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");
            documentUri = new Uri(applicationUri, document.DocumentUrl);
        }

        if (documentUri.Scheme != Uri.UriSchemeHttp && documentUri.Scheme != Uri.UriSchemeHttps)
        {
            return NotFound("Alamat dokumen tidak valid.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var remoteResponse = await client.GetAsync(
                documentUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!remoteResponse.IsSuccessStatusCode)
            {
                remoteResponse.Dispose();
                return NotFound("Dokumen tidak dapat diunduh.");
            }

            HttpContext.Response.RegisterForDispose(remoteResponse);

            var stream = await remoteResponse.Content.ReadAsStreamAsync(cancellationToken);
            var contentType = remoteResponse.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            var fileName = GetSafeFileName(document.OriginalFileName, documentUri);

            return File(stream, contentType, fileName);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Gagal mengunduh dokumen pricelist {contentId}: {ex}");
            return StatusCode(StatusCodes.Status502BadGateway, "Dokumen belum dapat diunduh. Silakan coba kembali.");
        }
    }

    private static string GetSafeFileName(string? originalFileName, Uri documentUri)
    {
        var fileName = Path.GetFileName(originalFileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = Path.GetFileName(Uri.UnescapeDataString(documentUri.AbsolutePath));
        }

        return string.IsNullOrWhiteSpace(fileName)
            ? "Dokumen-Persyaratan.pdf"
            : fileName;
    }

    private sealed class DownloadDocument
    {
        public string? DocumentUrl { get; init; }
        public string? OriginalFileName { get; init; }
    }

    public sealed class PermitPriceItem
    {
        public long ContentID { get; init; }
        public long PermitID { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? SubTitle { get; init; }
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public string? TypeService { get; init; }
        public string? DocumentPath { get; init; }
        public string? DocumentUrl { get; init; }
        public string? OriginalFileName { get; init; }
        public DateTime? UploadedDate { get; init; }

        public string PriceText => Price == 0
            ? "Gratis"
            : string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", Price);

        public string ServiceText => string.IsNullOrWhiteSpace(TypeService)
            ? "Perizinan"
            : TypeService;

        public string DownloadFileName => string.IsNullOrWhiteSpace(OriginalFileName)
            ? "Dokumen-Persyaratan"
            : OriginalFileName;
    }
}

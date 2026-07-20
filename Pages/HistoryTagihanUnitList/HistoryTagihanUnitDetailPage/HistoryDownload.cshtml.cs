using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList.HistoryTagihanUnitDetailPage
{
    [Authorize]
    public class HistoryDownloadModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HistoryDownloadModel> _logger;

        public HistoryDownloadModel(IHttpClientFactory httpClientFactory, ILogger<HistoryDownloadModel> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string fileKey, string documentType, string invoiceKey, string fileUrl, string invoiceNo)
        {
            fileUrl = DecodeBase64Url(fileKey) ?? fileUrl;
            invoiceNo = DecodeBase64Url(invoiceKey) ?? invoiceNo;

            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return NotFound("File belum tersedia untuk invoice ini.");
            }

            var normalizedDocumentType = string.Equals(documentType, "faktur-pajak", StringComparison.OrdinalIgnoreCase)
                ? "faktur-pajak"
                : "kwitansi";

            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var sourceUri) ||
                (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("URL file tidak valid.");
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, sourceUri);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0");
                request.Headers.Accept.ParseAdd("image/*,application/pdf,application/octet-stream,*/*;q=0.8");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    var sourceStatusCode = (int)response.StatusCode;
                    return StatusCode(sourceStatusCode, $"Server sumber gagal mengirim file. Status: {sourceStatusCode} {response.ReasonPhrase}");
                }

                var fileName = GetDownloadFileName(response, sourceUri, normalizedDocumentType, invoiceNo);
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    var provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(fileName, out contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                }

                return File(fileBytes, contentType, fileName);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Gagal mengambil file history tagihan dari server sumber.");
                return StatusCode(502, "Gagal mengambil file dari server sumber.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout saat mengambil file history tagihan dari server sumber.");
                return StatusCode(504, "Koneksi ke server sumber timeout.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal memproses download history tagihan.");
                return StatusCode(500, $"Gagal memproses download dokumen: {ex.Message}");
            }
        }

        private static string GetDownloadFileName(HttpResponseMessage response, Uri sourceUri, string documentType, string invoiceNo)
        {
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? Path.GetFileName(sourceUri.LocalPath);

            fileName = string.IsNullOrWhiteSpace(fileName)
                ? $"{documentType}-{invoiceNo}.pdf"
                : fileName.Trim('"');

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }

        private static string? DecodeBase64Url(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                var base64 = value.Replace('-', '+').Replace('_', '/');
                var padding = base64.Length % 4;

                if (padding > 0)
                {
                    base64 = base64.PadRight(base64.Length + 4 - padding, '=');
                }

                return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}

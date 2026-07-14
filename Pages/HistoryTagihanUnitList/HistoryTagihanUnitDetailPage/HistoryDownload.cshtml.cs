using System;
using System.IO;
using System.Net.Http;
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

        public HistoryDownloadModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> OnGetAsync(string fileUrl, string documentType, string invoiceNo)
        {
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

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    var sourceStatusCode = (int)response.StatusCode;
                    response.Dispose();
                    return StatusCode(sourceStatusCode, $"Server sumber gagal mengirim file. Status: {sourceStatusCode} {response.ReasonPhrase}");
                }

                HttpContext.Response.RegisterForDispose(response);
                var fileStream = await response.Content.ReadAsStreamAsync();
                var fileName = GetDownloadFileName(response, sourceUri, normalizedDocumentType, invoiceNo);
                var contentType = response.Content.Headers.ContentType?.MediaType;

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    var provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(fileName, out contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                }

                return File(fileStream, contentType, fileName);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, "Gagal mengambil file dari server sumber.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Koneksi ke server sumber timeout.");
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
    }
}

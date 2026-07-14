using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles; // Diperlukan untuk FileExtensionContentTypeProvider

namespace TestLandingPageNet8.Pages.HistoryTagihanUnitList.HistoryTagihanUnitDetailPage
{
    // Sesuaikan dengan struktur routing kamu
    [Route("HistoryTagihanUnitList/HistoryTagihanUnitDetailPage")]
    public class HistoryTagihanUnitDetailPageController : Controller
    {
        private readonly HttpClient _httpClient;

        // Menggunakan Dependency Injection untuk HttpClient (Best Practice)
        public HistoryTagihanUnitDetailPageController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("DownloadFileBypassCors")]
        public async Task<IActionResult> DownloadFileBypassCors([FromQuery] string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest("URL file tidak valid atau kosong.");
            }

            try
            {
                // 1. Ambil stream file dari URL sumber asli
                // Menggunakan HttpCompletionOption.ResponseHeadersRead agar tidak menimbun seluruh file di memori
                var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound("File tidak ditemukan di server sumber.");
                }

                // 2. Dapatkan stream data
                var fileStream = await response.Content.ReadAsStreamAsync();

                // 3. Ambil nama file asli dari URL untuk penamaan saat diunduh
                string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "downloaded_file";
                }

                // 4. Deteksi Content-Type (MIME Type) secara otomatis berdasarkan ekstensi file
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fileName, out string contentType))
                {
                    contentType = "application/octet-stream"; // Fallback jika ekstensi tidak dikenali
                }

                // 5. Kembalikan sebagai FileStreamResult ke browser
                // Ini akan langsung memicu download di browser user tanpa membebani RAM server kamu
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                // Log error ex disini sesuai kebutuhan
                return StatusCode(500, $"Terjadi kesalahan internal: {ex.Message}");
            }
        }
    }
}
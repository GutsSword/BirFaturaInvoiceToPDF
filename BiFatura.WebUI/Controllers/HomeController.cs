using BiFatura.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace BiFatura.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            var request = await _httpClient.PostAsJsonAsync("http://localhost:5136/api/Invoice/GetToken", loginViewModel);

            if (!request.IsSuccessStatusCode)
            {
                return Unauthorized(request.Content);
            }

            var response = request.Content.ReadFromJsonAsync<ClaimTokenViewModel>();
            TokenResponse.Token = response.Result.access_token;

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrWhiteSpace(TokenResponse.Token))
            {
                return Unauthorized();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenResponse.Token);

            var request = await _httpClient.PostAsync("http://localhost:5136/api/Invoice/GetSalesInformation", new StringContent(string.Empty));

            if (!request.IsSuccessStatusCode)
            {
                return BadRequest();
            }

            var response = await request.Content.ReadFromJsonAsync<List<InvoiceViewModel>>();

            if (!response.Any())
            {
                return NotFound();
            }

            return View(response);
        }

        [HttpGet]
        public async Task<IActionResult> ConvertToPdf(int id)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenResponse.Token);
            var response = await _httpClient.PostAsync($"http://localhost:5136/api/Invoice/ConvertToPdf?id={id}", default);

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            return File(pdfBytes, "application/pdf", $"BirFatura-{Guid.NewGuid()}.pdf");
        }
    }
}

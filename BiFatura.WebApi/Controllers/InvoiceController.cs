using BiFatura.WebApi.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace BiFatura.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        public static List<Invoice>? _invoiceList;

        public InvoiceController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        private const string grant_type = "grant_type";
        private const string grant_type_password = "password";

        [HttpPost("GetToken")]
        public async Task<IActionResult> GetToken(LoginUser loginUserDto)
        {
            if (loginUserDto.Username is null || loginUserDto.Password is null)
            {
                return BadRequest();
            }

            var credentials = new Dictionary<string, string>()
            {
               { grant_type, grant_type_password },
               { "username", loginUserDto.Username },
               { "password", loginUserDto.Password }
            };

            var content = new FormUrlEncodedContent(credentials);

            var request = await _httpClient.PostAsync("http://istest.birfatura.net/token", content);

            if (!request.IsSuccessStatusCode)
            {
                return Unauthorized("Kullanıcı adı veya şifre hatalı");
            }

            var response = await request.Content.ReadFromJsonAsync<ClaimToken>();

            TokenResponse.Token = response.access_token;

            return Ok(response);
        }

        [HttpPost("GetSalesInformation")]
        public async Task<IActionResult> GetSalesInformation()
        {
            if (string.IsNullOrWhiteSpace(TokenResponse.Token))
            {
                return Unauthorized();
            }

            var request = await GetAllInvoices();

            if (!request.IsSuccessStatusCode)
            {
                return BadRequest();
            }

            var response = await request.Content.ReadFromJsonAsync<List<Invoice>>();

            if (!response.Any())
            {
                return NotFound();
            }

            _invoiceList = response;

            return Ok(response);
        }

        [HttpPost("ConvertToPdf")]
        public async Task<IActionResult> ConvertToPdf(int id)
        {
            if (string.IsNullOrWhiteSpace(TokenResponse.Token))
            {
                return Unauthorized();
            }

            if (_invoiceList == null)
            {
                var request = await GetAllInvoices();

                if (request.IsSuccessStatusCode)
                {
                    _invoiceList = await request.Content.ReadFromJsonAsync<List<Invoice>>();
                }
                else
                {
                    return BadRequest("Faturalar getirilirken bir hata oluştu.");
                }
            }

            var invoice = _invoiceList?.Where(x => x.FaturaID == id).FirstOrDefault();

            if(_invoiceList == null)
            {
                return NotFound($"ID={id} fatura bulunamadı.");
            }

            var pdfStream = CreatePdf(invoice);

            if (pdfStream == null)
            {
                return BadRequest("PDF Oluşturulamadı.");
            }

            return File(pdfStream, MediaTypeNames.Application.Pdf, $"BirFatura_{Guid.NewGuid()}.pdf");
        }


        private MemoryStream CreatePdf(Invoice invoice)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                Document document = new Document(pageSize: PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, stream);
                writer.CloseStream = false;
                float sum=0;

                document.Open();
                // Birfatura logo
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(),"images", "images.png");
                Image birFaturaLogo = Image.GetInstance(logoPath); birFaturaLogo.ScaleToFit(150f, 150f);
                birFaturaLogo.Alignment = Element.ALIGN_LEFT;
                document.Add(birFaturaLogo);

                // title
                var titleFont = FontFactory.GetFont("Arial", 20, Font.BOLD);
                var title = new Paragraph("Fatura", titleFont) { Alignment = Element.ALIGN_CENTER };
                document.Add(title);

                document.Add(new Paragraph("\n"));

                // table detayları
                var table = new PdfPTable(2) { WidthPercentage = 100 };
                table.AddCell("Fatura ID:");
                table.AddCell(invoice.FaturaID.ToString());
                table.AddCell("Müsteri Adi:");
                table.AddCell(invoice.MusteriAdi);
                table.AddCell("Müsteri Adresi:");
                table.AddCell(invoice.MusteriAdresi);
                table.AddCell("Müsteri Telefon:");
                table.AddCell(invoice.MusteriTel);
                table.AddCell("Müsteri Sehir:");
                table.AddCell(invoice.MusteriSehir);
                table.AddCell("Müsteri TCVKN:");
                table.AddCell(invoice.MusteriTCVKN);
                table.AddCell("Müsteri Vergi Dairesi:");
                table.AddCell(invoice.MusteriVergiDairesi);
                document.Add(table);
                document.Add(new Paragraph("\n"));

                // satilmis urunler
                var titleFontSales = FontFactory.GetFont("Arial", 14, Font.BOLD);
                var titleSales = new Paragraph("Satilan Ürünler", titleFontSales) { Alignment = Element.ALIGN_CENTER };
                document.Add(titleSales);
                document.Add(new Paragraph("\n"));

                var productTable = new PdfPTable(6) { WidthPercentage = 100 };
                productTable.AddCell("Ürün ID");
                productTable.AddCell("Ürün Adı");
                productTable.AddCell("Stok Kodu");
                productTable.AddCell("Satis Adedi");
                productTable.AddCell("KDV Oranı");
                productTable.AddCell("KDV Dahil Birim Fiyatı");

                foreach (var saleProduct in invoice.SatilanUrunler)
                {
                    productTable.AddCell(saleProduct.UrunID.ToString());
                    productTable.AddCell(saleProduct.UrunAdi);
                    productTable.AddCell(saleProduct.StokKodu);
                    productTable.AddCell(saleProduct.SatisAdeti);
                    productTable.AddCell(saleProduct.KDVOrani);
                    productTable.AddCell(saleProduct.KDVDahilBirimFiyati);
                    sum += float.Parse(saleProduct.KDVDahilBirimFiyati) * float.Parse(saleProduct.SatisAdeti);
                }

                document.Add(productTable);
                document.Add(new Paragraph("\n"));

                var sumText = new Paragraph($"KDV Dahil Toplam Tutar: {sum} TL") { Alignment = Element.ALIGN_RIGHT };
                document.Add(sumText);

                document.Close();

                stream.Position = 0;
                return stream;
            }

            catch (Exception)
            {
                return null;
            }

        }

        private async Task<HttpResponseMessage> GetAllInvoices()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenResponse.Token);

            var request = await _httpClient.PostAsync("http://istest.birfatura.net/api/test/SatislarGetir", new StringContent(string.Empty));

            return request;
        }
    }
}

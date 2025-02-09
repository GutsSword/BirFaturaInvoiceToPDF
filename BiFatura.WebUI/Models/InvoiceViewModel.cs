namespace BiFatura.WebUI.Models
{
    public sealed class InvoiceViewModel
    {
        public int FaturaID { get; set; }
        public string MusteriAdi { get; set; }
        public string MusteriAdresi { get; set; }
        public string MusteriTel { get; set; }
        public string MusteriSehir { get; set; }
        public string MusteriTCVKN { get; set; }
        public string MusteriVergiDairesi { get; set; }

        public List<SalesViewModel>? SatilanUrunler { get; set; }
    }
}

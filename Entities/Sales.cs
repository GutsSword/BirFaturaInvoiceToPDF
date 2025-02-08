namespace BiFatura.WebApi.Models
{
    public class Sales
    {
        public int UrunID { get; set; }
        public string UrunAdi { get; set; }
        public string StokKodu { get; set; }
        public string SatisAdeti { get; set; }
        public string KDVOrani { get; set; }
        public string KDVDahilBirimFiyati { get; set; }
    }
}

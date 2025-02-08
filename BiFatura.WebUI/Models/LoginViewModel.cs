namespace BiFatura.WebUI.Models
{
    public sealed record LoginViewModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; } 
    }
}

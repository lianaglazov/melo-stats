using System.ComponentModel.DataAnnotations;

namespace MeloStats.Models
{
    public class SpotifyToken
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        [Required(ErrorMessage = "Access Token is required.")]
        public string AccessToken { get; set; }
        [Required(ErrorMessage = "Refresh Token is required.")]
        public string RefreshToken { get; set; }
        public int? ExpiresIn { get; set; }
        public string? TokenType { get; set; }
        public DateTime CreatedAt { get; set; }

        public ApplicationUser? User { get; set; }
    }
}

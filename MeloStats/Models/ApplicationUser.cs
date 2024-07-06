using System.ComponentModel.DataAnnotations;
namespace MeloStats.Models
{
    using Microsoft.AspNetCore.Identity;
    public class ApplicationUser : IdentityUser
    {
        public string? SpotifyUserId { get; set; }
        public virtual ICollection<SpotifyToken>? SpotifyTokens { get; set; }
        public virtual ICollection<ListeningHistory>? ListeningHistories { get; set; }
        public virtual ICollection<Playlist>? Playlists { get; set; }

    }
}

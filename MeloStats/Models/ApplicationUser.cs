using System.ComponentModel.DataAnnotations;
namespace MeloStats.Models
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Identity.Client;

    public class ApplicationUser : IdentityUser
    {
        public string SpotifyUserId { get; set; }
        // fotografia de profil
        public string? ImageUrl { get; set; }
        public virtual SpotifyToken? SpotifyToken { get; set; }
        public virtual ICollection<ListeningHistory>? ListeningHistories { get; set; }
        public virtual ICollection<Playlist>? Playlists { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;
namespace MeloStats.Models
{
    using Microsoft.AspNetCore.Identity;
    public class ApplicationUser : IdentityUser
    {
        public string? SpotifyUserId { get; set; }
        public virtual SpotifyToken? SpotifyToken { get; set; }
        public virtual ICollection<ListeningHistory>? ListeningHistories { get; set; }
        public virtual ICollection<Playlist>? Playlists { get; set; }

    }
}

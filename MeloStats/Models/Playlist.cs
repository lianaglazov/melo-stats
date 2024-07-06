using System.ComponentModel.DataAnnotations;

namespace MeloStats.Models
{
    public class Playlist
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "The Spotify Playlist ID is required.")]
        public string SpotifyPlaylistId { get; set; }
        [Required(ErrorMessage = "The name of the playlist is required.")]
        public string Name { get; set; }
        public virtual string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set;}
        public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; }
    }
}

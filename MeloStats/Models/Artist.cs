using System.ComponentModel.DataAnnotations;

namespace MeloStats.Models
{
    public class Artist
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "The Spotify ID of the artist is required.")]
        public string SpotifyArtistId { get; set; }
        [Required(ErrorMessage = "The name of the artist is required.")]
        public string Name { get; set; }
        public int Popularity { get; set; }
        public string Genres { get; set; }
        public string? ImageUrl { get; set; }
        public virtual ICollection<Track>? Tracks { get; set; }
        public virtual ICollection<Album>? Albums { get; set; }
    }
}

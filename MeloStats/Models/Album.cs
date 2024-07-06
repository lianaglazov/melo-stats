using System.ComponentModel.DataAnnotations;

namespace MeloStats.Models
{
    public class Album
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "The Spotify ID of the album is required.")]
        public string SpotifyAlbumId { get; set; }
        [Required(ErrorMessage = "The name of the album is required.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "The Release date is required.")]
        public DateTime ReleaseDate { get; set; }

        public virtual int? ArtistId { get; set; }
        public virtual Artist? Artist { get; set; }
        public virtual ICollection<Track>? Tracks { get; set; }
    }
}

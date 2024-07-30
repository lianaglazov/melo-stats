using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MeloStats.Models
{
    public class Track
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "The spotify id for the song is required.")]
        public string SpotifyTrackId { get; set; }
        [Required(ErrorMessage = "The name of the song is required.")]
        public string Name { get; set; }
        //the duration in seconds
        [Required(ErrorMessage = "Duration is required.")]
        public int Duration { get; set; }
        public int Popularity { get; set; }
        public int? ArtistId { get; set; }
        public int? AlbumId { get; set; }
        public virtual Artist? Artist { get; set; }
        public virtual Album? Album { get; set; }
        public virtual ICollection<ListeningHistory>? ListeningHistories { get; set; }
        public virtual ICollection<PlaylistTrack>? PlaylistTracks { get; set; }
    }
}

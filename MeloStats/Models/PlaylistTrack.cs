using System.ComponentModel.DataAnnotations.Schema;

namespace MeloStats.Models
{
    public class PlaylistTrack
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public virtual int? PlaylistId { get; set; }
        public virtual int? TrackId { get; set; }
        public virtual Playlist? Playlist { get; set; }
        public virtual Track? Track { get; set; }
    }
}

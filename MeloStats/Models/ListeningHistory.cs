using System.ComponentModel.DataAnnotations;

// tabel asociativ intre user si tracks, doar ca un user poate asculta
// un track de mai multe ori
namespace MeloStats.Models
{
    public class ListeningHistory
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? TrackId { get; set; }
        public DateTime PlayedAt { get; set; }

        public ApplicationUser? User { get; set; }
        public Track? Track { get; set; }
    }
}

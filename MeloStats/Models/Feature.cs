using System.ComponentModel.DataAnnotations;

namespace MeloStats.Models
{
    public class Feature
    {
        [Key]
        public int Id { get; set; }
        public float Danceability { get; set; }
        public float Energy { get; set; }
        public float Tempo { get; set; }
        public float Valence { get; set; }
        public float Instrumentalness { get; set; }
        public int? TrackId { get; set; }
        public virtual Track? Track { get; set; }

    }
}

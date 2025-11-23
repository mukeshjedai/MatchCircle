using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("matches", Schema = "best")]
    public class Match
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("user1_id")]
        public long User1Id { get; set; }

        [Required]
        [Column("user2_id")]
        public long User2Id { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("unmatched_at")]
        public DateTime? UnmatchedAt { get; set; }

        // Navigation properties
        [ForeignKey("User1Id")]
        public virtual User User1 { get; set; } = null!;

        [ForeignKey("User2Id")]
        public virtual User User2 { get; set; } = null!;

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}


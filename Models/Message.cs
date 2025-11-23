using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("messages", Schema = "best")]
    public class Message
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("match_id")]
        public long MatchId { get; set; }

        [Required]
        [Column("from_user_id")]
        public long FromUserId { get; set; }

        [Required]
        [Column("to_user_id")]
        public long ToUserId { get; set; }

        [Column("content", TypeName = "text")]
        public string? Content { get; set; }

        [MaxLength(50)]
        [Column("content_type")]
        public string ContentType { get; set; } = "text";

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MatchId")]
        public virtual Match Match { get; set; } = null!;

        [ForeignKey("FromUserId")]
        public virtual User FromUser { get; set; } = null!;

        [ForeignKey("ToUserId")]
        public virtual User ToUser { get; set; } = null!;
    }
}


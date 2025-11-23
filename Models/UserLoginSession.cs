using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("user_login_sessions", Schema = "best")]
    public class UserLoginSession
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("auth_token_hash")]
        public string AuthTokenHash { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("device_info")]
        public string? DeviceInfo { get; set; }

        [MaxLength(64)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}


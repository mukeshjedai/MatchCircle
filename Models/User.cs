using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("users", Schema = "best")]
    public class User
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(10)]
        [Column("phone_country_code")]
        public string? PhoneCountryCode { get; set; }

        [MaxLength(20)]
        [Column("phone_number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; } = false;

        [Column("is_phone_verified")]
        public bool IsPhoneVerified { get; set; } = false;

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "active";

        [MaxLength(20)]
        [Column("role")]
        public string Role { get; set; } = "user";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual UserProfile? UserProfile { get; set; }
        public virtual ICollection<UserLoginSession> LoginSessions { get; set; } = new List<UserLoginSession>();
        public virtual ICollection<ProfilePhoto> ProfilePhotos { get; set; } = new List<ProfilePhoto>();
        public virtual ICollection<UserVerification> Verifications { get; set; } = new List<UserVerification>();
        public virtual PartnerPreference? PartnerPreference { get; set; }
    }
}


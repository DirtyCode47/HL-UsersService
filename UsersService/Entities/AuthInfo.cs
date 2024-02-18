using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsersService.Entities
{
    public class AuthInfo
    {
        [Key]
        [ForeignKey("Users")]
        public Guid id { get; set; }

        [Required]
        [MaxLength(255)]
        public string role { get; set; }

        [Required]
        [MaxLength(30)]
        public string login { get; set; }

        [Required]
        public byte[] passwordHash { get; set; }

        [Required]
        public byte[] passwordSalt { get; set; }

        public Guid? jwtId { get; set; }
        public byte[]? refreshTokenHash { get; set; }
        public byte[]? refreshTokenSalt { get; set; }

        public DateTime? refreshTokenExpiry { get; set; }

        public virtual User Users { get; set; }
    }
}

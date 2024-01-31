using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsersService.Entities
{
    public class AuthInfo
    {
        [Key]
        [ForeignKey("User")]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string login { get; set; }

        [Required]
        public byte[] PasswordHash { get; set; }

        [Required]
        public byte[] PasswordSalt { get; set; }

        public Guid JwtId { get; set; }

        public virtual User User { get; set; }
    }
}

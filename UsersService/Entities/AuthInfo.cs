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
        [MaxLength(30)]
        public string login { get; set; }

        [Required]
        public byte[] password_hash { get; set; }

        [Required]
        public byte[] password_salt { get; set; }

        public Guid jwt_id { get; set; }

        public virtual User Users { get; set; }
    }
}

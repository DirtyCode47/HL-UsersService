using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.ComponentModel.DataAnnotations;

namespace UsersService.Entities
{
    public class User
    {
        [Key]
        public Guid id { get; set; } = Guid.NewGuid();

        [Required]
        public uint role { get; set; }

        [Required]
        [MaxLength(5)]
        public string post_code { get; set; }

        [Required]
        [MaxLength(255)]
        public string first_name { get; set; } 

        [Required]
        [MaxLength(255)]
        public string middle_name { get; set; }

        [Required]
        [MaxLength(255)]
        public string last_name { get; set; }

        [Required]
        [MaxLength(15)]
        public string phone { get; set; }


        [Required]
        [MaxLength(30)]
        public string login { get; set; }

        [Required]
        public byte[] PasswordHash { get; set; }
        
        [Required]
        public byte[] PasswordSalt { get; set; }

        public Guid JwtId { get; set; }
    }
}

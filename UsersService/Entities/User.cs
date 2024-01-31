using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace UsersService.Entities
{
    public class User
    {
        [Key]
        public Guid id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string role { get; set; }

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
    }
}

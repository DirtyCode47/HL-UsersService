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
        [MaxLength(5)]
        public string postCode { get; set; }

        [Required]
        [MaxLength(255)]
        public string firstName { get; set; } 

        [Required]
        [MaxLength(255)]
        public string middleName { get; set; }

        [Required]
        [MaxLength(255)]
        public string lastName { get; set; }

        [Required]
        [MaxLength(15)]
        public string phone { get; set; }
    }
}

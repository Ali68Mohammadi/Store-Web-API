using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models.Dto
{
    public class ContactDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = "";

        [Required, EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = "";


        [MaxLength(100)]
        public string? PhoneNumber { get; set; }

        public int SubjectId { get; set; }

        [Required]
        [MinLength(10), MaxLength(100)]
        public string Message { get; set; } = "";
    }
}

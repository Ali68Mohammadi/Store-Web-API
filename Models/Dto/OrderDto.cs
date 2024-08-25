using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models.Dto
{
    public class OrderDto
    {
        [Required]
        public string ProductIdentifiers { get; set; } = "";

        [Required, MinLength(20), MaxLength(100)]
        public string DeliverAddress { get; set; } = "";

        [Required, MaxLength(20)]
        public string PaymentMethod { get; set; } = "";

    }
}

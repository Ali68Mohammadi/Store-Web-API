﻿using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models.Dto
{
    public class UserProfileDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string PhoneNumber { get; set; } = "";

        public string Address { get; set; } = "";

        public string Role { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

    }
}

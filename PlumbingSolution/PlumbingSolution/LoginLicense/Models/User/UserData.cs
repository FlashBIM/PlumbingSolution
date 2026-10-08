using PlumbingSolution.LoginLicense.Enums;
using System;
using System.Collections.Generic;

namespace PlumbingSolution.LoginLicense.Models.User
{
    public class UserData
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public string Country { get; set; }
        public string PhoneNumber { get; set; }
        public string Picture { get; set; }
        public ICollection<string> Roles { get; set; } = new List<string>();
        public DateTime? LastLogin { get; set; }
        public UserStatus Status { get; set; }
    }
}
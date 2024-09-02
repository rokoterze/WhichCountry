using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WC.Models.DTO
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? PasswordHash { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}

using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace ProductService.Domain.Entites
{
    public class UserPreferences : BaseEntity
    {
        public string UserId { get; set; }
        public bool EmailNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public string Language { get; set; }
        public string Currency { get; set; }
        public string Theme { get; set; } // Light, Dark, System

        public User User { get; set; }
    }
}

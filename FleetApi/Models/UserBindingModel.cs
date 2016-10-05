using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FleetEntityFramework.Models;

namespace FleetApi.Models
{
    public class UserBindingModel
    {
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public UserRole Role { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FleetApi.Models
{
    public class UserLoginTokenModel
    {
        /// <summary>
        /// User first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// username: must be unqiue, generally maps to UoN || eduroam login
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Api access token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Time at which access token will expire
        /// </summary>
        public DateTime Expires { get; set; }
    }
}

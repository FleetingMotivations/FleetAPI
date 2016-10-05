using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FleetApi.Models
{
    public class WorkgroupBindingModel
    {
        public int UserId { get; set; }

        public int? RoomId { get; set; }

        public ICollection<int> AllowedApplications { get; set; }

        // Collection of workstation Id's to be in the workgroup
        public ICollection<int> Workstations { get; set; }

        // Duration in minutes. 
        public int Duration { get; set; }

        public bool SharingEnabled { get; set; }
    }
}
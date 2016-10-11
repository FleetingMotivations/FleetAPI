using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FleetApi.Models
{
    public class EntityModel
    {
        /// <summary>
        /// The database Id of the model
        /// </summary>
        public int Id { get; set; }
    }

    public class GenericItemModel : EntityModel 
    {
        /// <summary>
        /// The display name of the item
        /// </summary>
        public string Name { get; set; }
    }

    public class WorkstationModel : GenericItemModel
    {
        /// <summary>
        /// The last time the workstation announced itself
        /// as being available (talking to the WebService)
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// The IT issued identifier for the workstation
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// The percentage offset of the workstation relative to 
        /// the top of an image
        /// of a given room - x axis
        /// </summary>
        public float TopXRoomOffset { get; set; }

        /// <summary>
        /// The percentage offset of the workstation relative to 
        /// the top of an image
        /// of a given room - y axis
        /// </summary>
        public float TopYRoomOffset { get; set; }

        /// <summary>
        /// Indicates if a workstation is already part of a workgroup
        /// or otherwise unavailable for use (note that this does not
        /// mean a workstation is offline)
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// The colour associated with this workstation within its room
        /// </summary>
        public string Colour { get; set; }
    }

    public class WorkgroupMemberModel : WorkstationModel
    {
        /// <summary>
        /// Indicates if a given workstation can share content
        /// </summary>
        public bool SharingEnabled { get; set; }
    }

    public class WorkgroupModel
    {
        /// <summary>
        /// The time at which the workgroup was scheduled to start
        /// </summary>
        public DateTime Started { get; set; }

        /// <summary>
        /// The time at which the workgroup ended
        /// </summary>
        public DateTime Ended { get; set; }

        /// <summary>
        /// The amount of minutes the workgroup ran for
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// The applications that were permitted to run during the 
        /// workgroup
        /// </summary>
        public IEnumerable<GenericItemModel> AllowedApplications { get; set; } 

        /// <summary>
        /// The primary room from which the collaboration session took place
        /// </summary>
        public RoomDetailModel Room { get; set; }

        /// <summary>
        /// The workstations that were involved in the collaboration
        /// session when it ended
        /// </summary>
        public IEnumerable<WorkstationModel> Workstations { get; set; }
    }

    public class WorkgroupListingModel : EntityModel
    {
        /// <summary>
        /// The primary room from which the workgroup took place
        /// </summary>
        public GenericItemModel Room { get; set; }

        /// <summary>
        /// The time at which the workgroup started
        /// </summary>
        public DateTime Started { get; set; }
    }

    public class RoomDetailModel : GenericItemModel
    {
        /// <summary>
        /// The campus id for the room
        /// </summary>
        public int CampusId { get; set; }

        /// <summary>
        /// The building in which the room is
        /// </summary>
        public int BuildingId { get; set; }
    }
}
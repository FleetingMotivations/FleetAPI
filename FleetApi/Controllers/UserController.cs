using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using FleetApi.Models;
using FleetEntityFramework.DAL;

namespace FleetApi.Controllers
{
    [RoutePrefix("api")]
    public class UserController : BaseController
    {

        /// <summary>
        /// Returns a workgroup summary belonging to a given user
        /// 
        /// The summary is based on the state of the workgroup at the end of 
        /// the session
        /// 
        /// </summary>
        /// <param name="userId">The user that created the workgroup</param>
        /// <param name="workgroupId">The id of the workgroup to retrieve</param>
        /// <returns></returns>
        [HttpGet]
        [Route("users/{userId}/workgroups/{workgroupId}")]
        [ResponseType(typeof(WorkgroupModel))]
        public IHttpActionResult GetWorkgroup(int userId, int workgroupId)
        {
            using (var db = new FleetContext())
            {
                var workgroup = db.Workgroups
                    .Include(w => w.AllowedApplications)
                    .Include(w => w.Workstations.Select(wm => wm.Workstation))
                    .Include(w => w.Room)
                    .Where(w => w.UserId == userId)
                    .SingleOrDefault(w => w.WorkgroupId == workgroupId);

                if (workgroup == null)
                {
                    return Unauthorized();
                }

                return Ok(new WorkgroupModel
                {
                   Started = workgroup.Started,
                   Ended = workgroup.Expires,
                   Duration = (workgroup.Expires - workgroup.Started).Minutes,
                   AllowedApplications = workgroup.AllowedApplications
                        .Select(a => new GenericItemModel
                        {
                            Id = a.ApplicationId,
                            Name = a.ApplicationName
                        }),
                   Room = new RoomDetailModel {
                       Id = workgroup.RoomId.Value,
                       Name = workgroup.Room.RoomIdentifier,
                       CampusId = workgroup.Room.Building.CampusId,
                       BuildingId = workgroup.Room.BuildingId
                   },
                   Workstations = workgroup.Workstations
                        .Where(wm => !wm.TimeRemoved.HasValue)      
                           .Select(wm => new WorkstationModel
                           {
                               Id = wm.WorkstationId,
                               Name = wm.Workstation.FriendlyName,
                               Identifier = wm.Workstation.WorkstationIdentifier,
                               Colour = wm.Workstation.Colour,
                               TopXRoomOffset = wm.Workstation.TopXRoomOffset,
                               TopYRoomOffset = wm.Workstation.TopYRoomOffset,
                               LastSeen = wm.Workstation.LastSeen
                           })
                });
            }
        }

        /// <summary>
        /// Returns a list of prior workgroups that a user conducted
        /// </summary>
        /// <param name="userId">The user that conducted the workgroups</param>
        /// <param name="count">The amount of listings to retrive</param>
        /// <returns></returns>
        [HttpGet]
        [Route("users/{userId}/workgroups")]
        [ResponseType(typeof(IEnumerable<WorkgroupListingModel>))]
        public IHttpActionResult GetPriorWorkgroups(int userId, int? count = 5)
        {
            using (var db = new FleetContext())
            {
                var recentWorkgroups = db.Workgroups
                    .Include(w => w.Room)
                    .Where(w => w.UserId == userId)
                    .Where(w => w.Expires < DateTime.Now)
                    .OrderByDescending(w => w.Started)
                    .Take(count.Value)
                    .Select(wg => new WorkgroupListingModel
                    {
                        Id = wg.WorkgroupId,
                        Room = new GenericItemModel()
                        {
                            Id = wg.RoomId.Value,
                            Name = wg.Room.RoomIdentifier
                        },
                        Started = wg.Started
                    })
                    .ToList();

                return Ok(recentWorkgroups);
            }
        }

    }
}

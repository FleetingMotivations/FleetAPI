using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FleetEntityFramework.DAL;

namespace FleetApi.Controllers
{
    [RoutePrefix("api")]
    public class UserController : BaseController
    {
        [HttpGet]
        [Route("users/{userId}/workgroups/{workgroupId}")]
        public IHttpActionResult GetWorkgroup(int userId, int workgroupId)
        {
            using (var db = new FleetContext())
            {
                var workgroup = db.Workgroups
                    .Where(w => w.UserId == userId)
                    .SingleOrDefault(w => w.WorkgroupId == workgroupId);

                if (workgroup == null)
                {
                    return Unauthorized();
                }

                return Ok(new
                {
                   Started = workgroup.Started,
                   Expired = workgroup.Expires,
                   Duration = (workgroup.Expires - workgroup.Started).Minutes,
                   AllowedApplications = workgroup.AllowedApplications
                        .Select(a => new
                        {
                            Id = a.ApplicationId,
                            Name = a.ApplicationName
                        }),
                   Room = new {
                       Id = workgroup.RoomId,
                       Name = workgroup.Room.RoomIdentifier
                   },
                   Workstations = workgroup.Workstations
                        .Where(wm => !wm.TimeRemoved.HasValue)      
                           .Select(wm => new
                           {
                               Id = wm.WorkstationId,
                               FriendlyName = wm.Workstation.FriendlyName,
                               Colour = wm.Workstation.Colour,
                               TopXOffset = wm.Workstation.TopXRoomOffset,
                               TopYOffset = wm.Workstation.TopYRoomOffset,
                               LastSeen = wm.Workstation.LastSeen
                           })
                });
            }
        }

        [HttpGet]
        [Route("users/{userId}/workgroups")]
        public IHttpActionResult GetPriorWorkgroups(int userId, int? count = 5)
        {
            using (var db = new FleetContext())
            {
                var recentWorkgroups = db.Workgroups
                    .Where(w => w.UserId == userId)
                    .Where(w => w.Expires < DateTime.Now)
                    .OrderByDescending(w => w.Started)
                    .Take(count.Value)
                    .Select(wg => new
                    {
                        Id = wg.WorkgroupId,
                        Room = new
                        {
                            Id = wg.RoomId,
                            Name = wg.Room.RoomIdentifier
                        },
                        Started = wg.Started
                    })
                    .ToList();

                return Ok(recentWorkgroups);
            }
            return NotFound();
        }

    }
}

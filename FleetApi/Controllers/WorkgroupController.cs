using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FleetApi.Models;
using FleetEntityFramework.DAL;
using FleetEntityFramework.Models;
using WebGrease.Css.Extensions;

namespace FleetApi.Controllers
{
    [RoutePrefix("api")]
    public class WorkgroupController : BaseController
    {
        private readonly static object _lock = new object();

        [HttpPut]
        [Route("workgroup/{workgroupId}/workstation/{workstationId}/sharing")]
        public IHttpActionResult ToggleSharingAbility(int workgroupId, int workstationId, [FromBody]bool enable)
        {
            using (var db = new FleetContext())
            {
                // Ensure that the workstation is in the workgroup, and hasn't been removed
                var workstationGroupMembership = db.WorkgroupMembers
                    .Where(w => w.WorkstationId == workstationId)
                    .Where(w => w.WorkgroupId == workgroupId)
                    .Where(w => !w.TimeRemoved.HasValue)
                    .Where(Workgroup.IsInProgress())
                    .SingleOrDefault();

                if (workstationGroupMembership == null)
                {
                    return Unprocessable(new { error = "Invalid workstation or workgroup"});
                }

                workstationGroupMembership.SharingEnabled = enable;

                db.SaveChanges();

                return Ok();
            }
        }

        [HttpGet]
        [Route("workgroup/workstations")]
        public IHttpActionResult GetWorkstations(int workgroupId)
        {
            return Ok();
        }


        [HttpPost]
        [Route("workgroup")]
        public IHttpActionResult CreateWorkgroup(WorkgroupBindingModel workgroup)
        {
            using (var db = new FleetContext())
            {
                Room room = null;

                // If there is a room, check its valid
                if (workgroup.RoomId.HasValue)
                {
                    room = db.Rooms.FirstOrDefault(r => r.RoomId == workgroup.RoomId.Value);
                    if (room == null)
                    {
                        // Invalid room Id provided, don't continue
                        return Unprocessable(new
                        {
                            error = "Room not found"
                        });
                    }
                }

                // Check a valid user is making the request
                var commisioner = db.Users.FirstOrDefault(u => u.UserId == workgroup.UserId);
                if (commisioner == null)
                {
                    return Unprocessable(new
                    {
                        error = "Provided user does not exist"
                    });
                }

                // Obtain a lock before determining if workstations are valid
                // We need the lock to ensure that workstations that are 
                // determined to be available are available when added
                Workgroup workgroupModel;
                lock (_lock)
                {
                    // Ensure all selected workstations are not currently in a group
                    var badWorkstations = db.WorkgroupMembers
                        .Where(wm => workgroup.Workstations.Contains(wm.WorkstationId))
                        .Where(wm => !wm.TimeRemoved.HasValue)
                        .Where(Workgroup.IsInProgress())
                        .Select(ww => new
                        {
                            Id = ww.WorkstationId
                        })
                        .ToList();

                    if (badWorkstations.Any())
                    {
                        return Unprocessable(new
                        {
                            unavailableWorkstations = badWorkstations
                        });
                    }

                    workgroupModel = new Workgroup
                    {
                        UserId = workgroup.UserId,
                        Started = DateTime.Now,
                        Expires = DateTime.Now.AddMinutes(workgroup.Duration),
                        RoomId = room?.RoomId,
                        AllowedApplications =
                            new List<Application>
                            {
                                db.Applications
                                    .Single(a => a.ApplicationName == "FileSharer")
                            },

                    };

                    db.Workgroups.Add(workgroupModel);
                    db.SaveChanges();

                    var mappings = db.Workstations
                        .Where(w => workgroup.Workstations.Contains(w.WorkstationId))
                        .Select(w => w.WorkstationId)
                        .ToList()
                        .Select(id => new WorkgroupWorkstation
                        {
                            WorkstationId = id,
                            WorkgroupId = workgroupModel.WorkgroupId,
                            TimeRemoved = null,
                            SharingEnabled = workgroup.SharingEnabled
                        });
                        
                    mappings.ForEach(ww => db.WorkgroupMembers.Add(ww));

                    db.SaveChanges();
                }
                return Ok(new
                {
                    WorkgroupdId = workgroupModel.WorkgroupId
                });
            }
        }
    }
}

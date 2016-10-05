using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        [Route("workgroups/{workgroupId}/workstations/{workstationId}/sharing")]
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

        [HttpPut]
        [Route("workgroups/{workgroupId}/sharing")]
        public IHttpActionResult ToggleSharingAbility(int workgroupId, [FromBody]bool enable)
        {
            using (var db = new FleetContext())
            {
                // Ensure that the workstation is in the workgroup, and hasn't been removed
                var workstationGroupMemberships = db.WorkgroupMembers
                    .Where(w => w.WorkgroupId == workgroupId)
                    .Where(Workgroup.IsInProgress())
                    .Where(w => !w.TimeRemoved.HasValue);

                workstationGroupMemberships.ForEach(w => w.SharingEnabled = enable);
                db.SaveChanges();

                return Ok();
            }
        }

        [HttpPost]
        [Route("workgroups/{workgroupId}/workstations/{workstationId}")]
        public IHttpActionResult AddWorkstation(int workgroupId, int workstationId)
        {
            using (var db = new FleetContext())
            {
                 var workgroupInProgress = db.Workgroups
                    .Where(w => w.WorkgroupId == workgroupId)
                    .SingleOrDefault(Workgroup.InProgress()) != null;

                if (!workgroupInProgress)
                {
                    return Unprocessable(new
                    {
                        error = "Workgroup not in progress"
                    });
                }

                // Need that lock again to ensure that we aren't double adding workstations
                // due to racey conditions D:
                lock (_lock)
                {
                    var unavailable = db.Workstations
                    .Include(w => w.Workgroups.Select(wm => wm.Workgroup))
                    .Single(w => w.WorkstationId == workstationId)
                    .Workgroups
                    .Where(wm => !wm.TimeRemoved.HasValue)
                    .AsQueryable()
                    .Any(Workgroup.IsInProgress());

                    if (unavailable)
                    {
                        return Unprocessable(new
                        {
                            error = "Workstation is unavailable"
                        });
                    }

                    // At this point, we know the workstation is available, and the workgroup is in progress
                    db.WorkgroupMembers.Add(new WorkgroupWorkstation
                    {
                        WorkgroupId = workgroupId,
                        WorkstationId = workstationId,
                        TimeAdded = DateTime.Now,
                        TimeRemoved = null,
                        SharingEnabled = true
                    });

                    db.SaveChanges();
                }
            }
            return NotFound();
        }

        [HttpDelete]
        [Route("workgroups/{workgroupId}/workstations/{workstationId}")]
        public IHttpActionResult DeleteWorkstation(int workgroupId, int workstationId)
        {
            using (var db = new FleetContext())
            {
                var workgroupMember = db.WorkgroupMembers
                    .Where(w => w.WorkgroupId == workgroupId)
                    .Where(w => w.WorkstationId == workstationId)
                    .Where(Workgroup.IsInProgress())
                    .SingleOrDefault(w => !w.TimeRemoved.HasValue);

                if (workgroupMember == null)
                {
                    return Unprocessable(new
                    {
                        error = "Workstation not part of workgroup. Workgroup may have ended"
                    });
                }

                // Otherwise, we can safely remove them. This doesn't need a lock
                // as it's not going to lead to double allocation of a workstation
                // to a workgroup
                workgroupMember.TimeRemoved = DateTime.Now;

                db.SaveChanges();
                return Ok();
            }
        }

        [HttpGet]
        [Route("workgroups/{workgroupId}/workstations")]
        public IHttpActionResult GetWorkstations(int workgroupId)
        {
            using (var db = new FleetContext())
            {
                var workstations = db.Workgroups
                    .Include(w => w.Workstations.Select(wm => wm.Workgroup))
                    .Single(w => w.WorkgroupId == workgroupId)
                    .Workstations
                    .Where(wm => !wm.TimeRemoved.HasValue)
                    .Select(w => new
                    {
                        Id = w.WorkstationId,
                        SharingEnabled = w.SharingEnabled,
                        LastSeen = w.Workstation.LastSeen,
                        FriendlyName = w.Workstation.FriendlyName,
                        Colour = w.Workstation.Colour,
                        TopXOffset = w.Workstation.TopXRoomOffset,
                        TopYOffset = w.Workstation.TopYRoomOffset
                    })
                    .ToList();
                return Ok(workstations);
            }
        }

            
        [HttpPost]
        [Route("workgroups")]
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
                            id = ww.WorkstationId
                        })
                        .ToList();

                    if (badWorkstations.Any())
                    {
                        return Unprocessable(new
                        {
                            error = "Workstations Unavailable",
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
                            SharingEnabled = workgroup.SharingEnabled,
                            TimeAdded = DateTime.Now
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

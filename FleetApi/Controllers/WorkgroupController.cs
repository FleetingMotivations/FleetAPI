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
using FleetEntityFramework.Models;
using WebGrease.Css.Extensions;

namespace FleetApi.Controllers
{
    [RoutePrefix("api")]
    public class WorkgroupController : BaseController
    {
        private readonly static object _lock = new object();

        /// <summary>
        /// Enable of disable sharing capabilities for a particular workstation in a workgroup
        /// </summary>
        /// <param name="workgroupId">THe target workgroup</param>
        /// <param name="workstationId">THe target workstation</param>
        /// <param name="enable">Toggle value</param>
        /// <returns></returns>
        [HttpPut]
        [Route("workgroups/{workgroupId}/workstations/{workstationId}/sharing")]
        public IHttpActionResult ToggleSharingAbility(int workgroupId, int workstationId, [FromBody]bool enable)
        {
            using (var db = new FleetContext())
            {
                // Ensure that the workstation is in the workgroup, and hasn't been removed
                var workstationGroupMembership = db.WorkgroupMembers
                    .Include(w => w.Workstation)
                    .Include(w => w.Workgroup)
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

        /// <summary>
        /// Enables or disables sharing for the whole workgroup
        /// </summary>
        /// <param name="workgroupId">THe target workgroup</param>
        /// <param name="enable">Toggle value</param>
        /// <returns></returns>
        [HttpPut]
        [Route("workgroups/{workgroupId}/sharing")]
        public IHttpActionResult ToggleSharingAbility(int workgroupId, [FromBody]bool enable)
        {
            using (var db = new FleetContext())
            {
                // Ensure that the workstation is in the workgroup, and hasn't been removed
                var workstationGroupMemberships = db.WorkgroupMembers
                    .Include(w => w.Workgroup)
                    .Where(w => w.WorkgroupId == workgroupId)
                    .Where(Workgroup.IsInProgress())
                    .Where(w => !w.TimeRemoved.HasValue);

                workstationGroupMemberships.ForEach(w => w.SharingEnabled = enable);
                db.SaveChanges();

                return Ok();
            }
        }

        /// <summary>
        /// 
        /// Adds a workstation to the workgroup
        /// Assumes the workgroup is in progress, and the workstation is available
        /// 
        /// </summary>
        /// <param name="workgroupId">THe target workgroup</param>
        /// <param name="workstationId">THe target workstation</param>
        /// <returns></returns>
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

        /// <summary>
        /// Will remove a given workstation from the workgroup
        /// 
        /// Assumes workgroup is still in progress, and that workstation is
        /// currently in the workgroup
        ///  
        /// </summary>
        /// <param name="workgroupId">THe target workgroup</param>
        /// <param name="workstationId">THe target workstation</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("workgroups/{workgroupId}/workstations/{workstationId}")]
        public IHttpActionResult DeleteWorkstation(int workgroupId, int workstationId)
        {
            using (var db = new FleetContext())
            {
                var workgroupMember = db.WorkgroupMembers
                    .Include(w => w.Workgroup)
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

        /// <summary>
        /// Returns all of the workstations that are currently a member of the workgroup
        /// </summary>
        /// <param name="workgroupId">The target workgroup</param>
        /// <returns></returns>
        [HttpGet]
        [Route("workgroups/{workgroupId}/workstations")]
        [ResponseType(typeof(WorkgroupMemberModel))]
        public IHttpActionResult GetWorkstations(int workgroupId)
        {
            using (var db = new FleetContext())
            {
                var workstations = db.Workgroups
                    .Include(w => w.Workstations.Select(wm => wm.Workgroup))
                    .Single(w => w.WorkgroupId == workgroupId)
                    .Workstations
                    .Where(wm => !wm.TimeRemoved.HasValue)
                    .Select(w => new WorkgroupMemberModel
                    {
                        Id = w.WorkstationId,
                        SharingEnabled = w.SharingEnabled,
                        LastSeen = w.Workstation.LastSeen,
                        Name = w.Workstation.FriendlyName,
                        Colour = w.Workstation.Colour,
                        TopXRoomOffset = w.Workstation.TopXRoomOffset,
                        TopYRoomOffset = w.Workstation.TopYRoomOffset,
                        Identifier = w.Workstation.WorkstationIdentifier
                    })
                    .ToList();
                return Ok(workstations);
            }
        }

        /// <summary>
        /// Creates a new workgroup
        /// 
        /// Assumes all of the workstations are currently available, and that the user
        /// making the workgroup, doesn't have any currently running or scheduled workgroups
        /// 
        /// </summary>
        /// <param name="workgroup"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("workgroups")]
        [ResponseType(typeof(EntityModel))]
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
                        .Include(w => w.Workgroup)
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
                            TimeAdded = DateTime.Now,
                        });
                        
                    mappings.ForEach(ww => db.WorkgroupMembers.Add(ww));

                    db.SaveChanges();
                }
                return Ok(new EntityModel
                {
                    Id = workgroupModel.WorkgroupId
                });
            }
        }
    }
}

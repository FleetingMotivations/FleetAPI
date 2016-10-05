using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using FleetApi.Models;
using FleetEntityFramework.DAL;
using FleetEntityFramework.Models;
using WebGrease.Css.Extensions;
using Route = System.Web.Http.RouteAttribute;
using RoutePrefix = System.Web.Http.RoutePrefixAttribute;

using HttpPost = System.Web.Http.HttpPostAttribute;
using HttpGet = System.Web.Http.HttpGetAttribute;

namespace FleetApi.Controllers
{
    [RoutePrefix("api")]
    public class HomeController : BaseController
    {
        public IHttpActionResult Index()
        {
            return RedirectToRoute("/help", null);
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(string username, string password)
        {
            using (var db = new FleetContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Identifer == username);
                if (user == null)
                {
                    return Unprocessable(new { error = "User not found" });
                }
                return Ok(new
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Identifer,
                    Token = "nonsense"
                });
            }
        }

        [HttpGet]
        [Route("campuses")] // Stupid plurals
        public IHttpActionResult GetCampuses()
        {
            using (var db = new FleetContext())
            {
                var campuses = db.Campuses
                    .Select(c => new
                    {
                        Id = c.CampusId,
                        CampusName = c.CampusIdentifer
                    })
                    .ToList();

                return Ok(campuses);
            }
        }

        [HttpGet]
        [Route("campuses/{campusId}/buildings")]
        public IHttpActionResult GetBuildings(int campusId)
        {
            using (var db = new FleetContext())
            {
                var buildings = db.Buildings
                    .Where(b => b.CampusId == campusId)
                    .Select(c => new
                    {
                        Id = c.BuildingId,
                        BuildingName = c.BuildingIdentifier
                    })
                    .ToList();

                return Ok(buildings);
            }
        }

        [HttpGet]
        [Route("buildings/{buildingId}/rooms")]
        public IHttpActionResult GetRooms(int buildingId)
        {
            using (var db = new FleetContext())
            {
                var rooms = db.Rooms
                    .Where(r => r.BuildingId == buildingId)
                    .Select(r => new
                    {
                        Id = r.RoomId,
                        RoomName = r.RoomIdentifier
                    })
                    .ToList();

                return Ok(rooms);
            }
        }

        [HttpGet]
        [Route("rooms/{roomId}/workstations")]
        public IHttpActionResult GetWorkstations(int roomId)
        {
            using (var db = new FleetContext())
            {
                // Don't need a lock here because we are only reading
                // It will fail later if there are race conditions
                var availableWorkstationModels = db.Workstations
                    .Where(w => w.RoomId == roomId)
                    .Where(w => (!w.Workgroups.Any()) || w.Workgroups
                        .All(wgr =>
                            wgr.Workgroup.Started > DateTime.Now    // Started in the fuxture
                            || wgr.Workgroup.Expires < DateTime.Now // Ended in the past
                            || wgr.TimeRemoved.HasValue             // Or was removed from a workgroup
                        )
                    )
                    .Select(w => new
                    {
                        Id = w.WorkstationId,
                        Name = w.FriendlyName,
                        LastSeen = w.LastSeen,
                        TopXRoomOffset = w.TopXRoomOffset,
                        TopYRoomOffset = w.TopYRoomOffset,
                        Available = true
                    });

                var unavailableWorkstationModels = db.Workstations
                    .Where(w => w.RoomId == roomId)
                    .Where(w => w.Workgroups
                        .Any(wgr =>
                            wgr.Workgroup.Started < DateTime.Now    // Started in the fuxture
                            && wgr.Workgroup.Expires > DateTime.Now // Ended in the past
                            && !wgr.TimeRemoved.HasValue             // Or was removed from a workgroup
                        )
                    )
                    .Select(w => new
                    {
                        Id = w.WorkstationId,
                        Name = w.FriendlyName,
                        LastSeen = w.LastSeen,
                        TopXRoomOffset = w.TopXRoomOffset,
                        TopYRoomOffset = w.TopYRoomOffset,
                        Available = false
                    });

                var q = availableWorkstationModels.Union(unavailableWorkstationModels).ToList();
                return Ok(q);
            }
        }
    }
}

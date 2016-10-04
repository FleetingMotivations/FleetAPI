using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using FleetEntityFramework.DAL;
using FleetEntityFramework.Models;
using Route = System.Web.Http.RouteAttribute;
using RoutePrefix = System.Web.Http.RoutePrefixAttribute;

namespace FleetApi.Controllers
{
    [RoutePrefix("api")]
    public class HomeController : ApiController
    {
        public IHttpActionResult Index()
        {
            return Ok("hello");
        }

        [Route("campuses")]
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

        [Route("buildings/{campusId}")]
        public IHttpActionResult GetBuildings(int campusId)
        {
            using (var db = new FleetContext())
            {
                var buildings = db.Buildings
                    .Where(b => b.CampusId == campusId)
                    .Select(c => new
                    {
                        Id = c.BuildingId,
                        CampusName = c.BuildingIdentifier
                    })
                    .ToList();

                return Ok(buildings);
            }
        }
            
        [Route("rooms/{buildingId}")]
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

        [Route("workstations/{roomId}")]
        public IHttpActionResult GetWorkstations(int roomId)
        {
            using (var db = new FleetContext())
            {
                var availableWorkstationModels = db.Workstations
                    .Where(w => w.RoomID == roomId)
                    .Where(w => (!w.Workgroups.Any()) || w.Workgroups
                        .All(wgr => 
                            wgr.Workgroup.Started > DateTime.Now    // Started in the fuxture
                            || wgr.Workgroup.Expires < DateTime.Now // Ended in the past
                            || wgr.TimeRemoved.HasValue             // Or was removed from a workgroup
                        )
                    ).Select(w => new
                    {
                        Id = w.WorkstationId,
                        Name = w.FriendlyName,
                        LastSeen = w.LastSeen,
                        TopXRoomOffset = w.TopXRoomOffset,
                        TopYRoomOffset = w.TopYRoomOffset,
                        Available = true
                    });
                var unavailableWorkstationModels = db.Workstations
                    .Where(w => w.RoomID == roomId)
                    .Where(w => w.Workgroups
                        .Any(wgr => 
                            wgr.Workgroup.Started < DateTime.Now    // Started in the fuxture
                            && wgr.Workgroup.Expires > DateTime.Now // Ended in the past
                            && !wgr.TimeRemoved.HasValue             // Or was removed from a workgroup
                        )
                    ).Select(w => new
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

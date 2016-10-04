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

        [Route("buildings")]
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
            
        [Route("rooms")]
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
    }
}

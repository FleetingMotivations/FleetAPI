using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FleetApi.Models;
using FleetEntityFramework.DAL;
using FleetEntityFramework.Models;

namespace FleetApi.Controllers
{
    [RoutePrefix("api/admin")]
    public class AdminController : ApiController
    {

        [Route("campus")]
        [HttpPost]
        public IHttpActionResult AddCampus(string campusName)
        {
            using (var db = new FleetContext())
            {
                db.Campuses.Add(new Campus
                {
                    CampusIdentifer = campusName
                });
                db.SaveChanges();
                return Ok();
            }
        }

        [Route("building")]
        [HttpPost]
        public IHttpActionResult AddBuilding(string buildingName, int campusId)
        {
            using (var db = new FleetContext())
            {
                db.Buildings.Add(new Building
                {
                    BuildingIdentifier = buildingName,
                    CampusId = campusId
                });
                db.SaveChanges();
                return Ok();
            }
        }

        [Route("room")]
        [HttpPost]
        public IHttpActionResult AddRoom(string roomName, int buildingId)
        {
            using (var db = new FleetContext())
            {
                db.Rooms.Add(new Room
                {
                    RoomIdentifier = roomName,
                    BuildingId = buildingId
                });
                db.SaveChanges();
                return Ok();
            }
        }

        [Route("workstation")]
        [HttpPost]
        public IHttpActionResult AddWorkstation(WorkstationBindingModel model)
        {
            using (var db = new FleetContext())
            {
                var workstation = new Workstation
                {
                    WorkstationIdentifier = model.WorkstationIdentifier,
                    RoomId = model.RoomId,
                    FriendlyName = model.FriendlyName,
                    LastSeen = DateTime.Today

                };
                db.Workstations.Add(workstation);
                db.SaveChanges();
                return Ok(workstation.WorkstationId);
            }
        }

        [Route("userRoles")]
        public IHttpActionResult GetRoles()
        {
            return Ok(new
            {
                Facilitator = 1,
                Regular = 2
            });
        }

       

        [Route("unsafe/user")]
        [HttpPost]
        public IHttpActionResult AddUser(string username, string firstName, string lastName, UserRole role)
        {
            using (var db = new FleetContext())
            {
                db.Users.Add(new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Identifer = username,
                    Role = role
                });

                db.SaveChanges();

                return Ok();
            }
        }
    }
}

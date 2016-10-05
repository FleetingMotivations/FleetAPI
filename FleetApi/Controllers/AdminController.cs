using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using FleetApi.Models;
using FleetEntityFramework.DAL;
using FleetEntityFramework.Models;

namespace FleetApi.Controllers
{
    [RoutePrefix("api/admin")]
    public class AdminController : ApiController
    {
        /// <summary>
        /// Adds a new campus
        /// </summary>
        /// <param name="campusName">The name of the campus for display</param>
        /// <returns>HTTP 200 or 500 if error</returns>
        [Route("campuses")]
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

        /// <summary>
        /// Adds a new building to the given campus
        /// </summary>
        /// <param name="buildingName">The name of the building for display</param>
        /// <param name="campusId">The id of the campus where the building is</param>
        /// <returns>200 || 500 if error</returns>
        [Route("campuses/{campusId}/buildings")]
        [HttpPost]
        public IHttpActionResult AddBuilding(int campusId, [FromBody] string buildingName)
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

        /// <summary>
        /// Adds a room to the given building
        /// </summary>
        /// <param name="buildingId">Building to add room to</param>
        /// <param name="roomName">Display name for the room</param>
        /// <returns>200 || 500 if error</returns>
        [Route("buildings/{buildingId}/rooms")]
        [HttpPost]
        public IHttpActionResult AddRoom(int buildingId, [FromBody] string roomName)
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

        /// <summary>
        /// Adds a workstation to the given room
        /// </summary>
        /// <param name="roomId">The room to add the workstation to</param>
        /// <param name="model">Unique identifier and a display name for the workstation</param>
        /// <returns>workstationId || 500 if error</returns>
        [Route("rooms/{roomId}/workstations")]
        [HttpPost]
        [ResponseType(typeof(EntityModel))]
        public IHttpActionResult AddWorkstation(int roomId, [FromBody] WorkstationBindingModel model)
        {
            using (var db = new FleetContext())
            {
                var workstation = new Workstation
                {
                    WorkstationIdentifier = model.WorkstationIdentifier,
                    RoomId = roomId,
                    FriendlyName = model.FriendlyName,
                    LastSeen = DateTime.Today

                };
                db.Workstations.Add(workstation);
                db.SaveChanges();
                return Ok(new EntityModel
                {
                    Id = workstation.WorkstationId
                });
            }
        }

        /// <summary>
        /// Returns the possible roles a user can have within the application
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("userRoles")]
        public IHttpActionResult GetRoles()
        {
            return Ok(new
            {
                Facilitator = 1,
                Regular = 2
            });
        }
        
        /// <summary>
        /// Adds a mock user to the system. This would be replaced with soft identity 
        /// derived from a UoN single sign on in production
        /// </summary>
        /// <param name="model">User details</param>
        /// <returns></returns>
        [HttpPost]
        [Route("users")]
        public IHttpActionResult AddUser([FromBody] UserBindingModel model)
        {
            using (var db = new FleetContext())
            {
                db.Users.Add(new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Identifer = model.Username,
                    Role = model.Role
                });

                db.SaveChanges();

                return Ok();
            }
        }
    }
}

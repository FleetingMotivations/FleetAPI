/* 
 * Description: Provides base REST like functionality and access to various resources
 *
 * Project: FleetApi
 * Group Members: Jordan Collins, Tristan Newmann, Hayden Cheers, Alistair Woodcock
 * Last modified: 11/10/16
 * Last Author: Tristan Newmann
 * 
*/

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
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

        /// <summary>
        /// Login to the system with UoN credentials
        /// </summary>
        /// <param name="username">UoN username</param>
        /// <param name="password">UoN password</param>
        /// <returns></returns>
        [HttpPost]
        [Route("login")]
        [ResponseType(typeof(UserLoginTokenModel))]
        public IHttpActionResult Login(string username, string password)
        {
            using (var db = new FleetContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Identifer == username);
                if (user == null)
                {
                    return Unprocessable(new { error = "User not found" });
                }
                return Ok(new UserLoginTokenModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Identifer,
                    Token = "nonsense",
                    Expires = DateTime.Today.AddDays(30)
                });
            }
        }

        /// <summary>
        /// Returns a list of campus'
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("campuses")] // Stupid plurals
        [ResponseType(typeof(IEnumerable<GenericItemModel>))]
        public IHttpActionResult GetCampuses()
        {
            using (var db = new FleetContext())
            {
                var campuses = db.Campuses
                    .Select(c => new GenericItemModel
                    {
                        Id = c.CampusId,
                        Name = c.CampusIdentifer
                    })
                    .ToList();

                return Ok(campuses);
            }
        }

        /// <summary>
        /// Returns all of the buildings on a given campus
        /// </summary>
        /// <param name="campusId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("campuses/{campusId}/buildings")]
        [ResponseType(typeof(IEnumerable<GenericItemModel>))]
        public IHttpActionResult GetBuildings(int campusId)
        {
            using (var db = new FleetContext())
            {
                var buildings = db.Buildings
                    .Where(b => b.CampusId == campusId)
                    .Select(c => new GenericItemModel
                    {
                        Id = c.BuildingId,
                        Name = c.BuildingIdentifier
                    })
                    .ToList();

                return Ok(buildings);
            }
        }

        /// <summary>
        /// Returns a collection of all the rooms in a given building
        /// </summary>
        /// <param name="buildingId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("buildings/{buildingId}/rooms")]
        [ResponseType(typeof(IEnumerable<GenericItemModel>))]
        public IHttpActionResult GetRooms(int buildingId)
        {
            using (var db = new FleetContext())
            {
                var rooms = db.Rooms
                    .Where(r => r.BuildingId == buildingId)
                    .Select(r => new GenericItemModel
                    {
                        Id = r.RoomId,
                        Name = r.RoomIdentifier
                    })
                    .ToList();

                return Ok(rooms);
            }
        }

        /// <summary>
        /// Returns a collection of all of the workstations for a given room
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("rooms/{roomId}/workstations")]
        [ResponseType(typeof(IEnumerable<WorkstationModel>))]
        public IHttpActionResult GetWorkstations(int roomId)
        {
            using (var db = new FleetContext())
            {
                var baseWorkstations = db.Workstations
                    .Include(w => w.Room)
                    .Include(w => w.Workgroups.Select(wm => wm.Workgroup))
                    .Where(w => w.RoomId == roomId);
                    
                // Don't need a lock here because we are only reading
                // It will fail later if there are race conditions
                var availableWorkstationModels = baseWorkstations
                    .Where(w => (!w.Workgroups.Any()) || w.Workgroups
                        .All(wgr =>
                            wgr.Workgroup.Started > DateTime.Now    // Started in the fuxture
                            || wgr.Workgroup.Expires < DateTime.Now // Ended in the past
                            || wgr.TimeRemoved.HasValue             // Or was removed from a workgroup
                        )
                    )
                    .Select(w => new WorkstationModel
                    {
                        Id = w.WorkstationId,
                        Name = w.FriendlyName,
                        LastSeen = w.LastSeen,
                        TopXRoomOffset = w.TopXRoomOffset,
                        TopYRoomOffset = w.TopYRoomOffset,
                        Available = true
                    });

                var unavailableWorkstationModels = baseWorkstations
                    .Where(w => w.Workgroups
                        .Any(wgr =>
                            wgr.Workgroup.Started < DateTime.Now    // Started in the fuxture
                            && wgr.Workgroup.Expires > DateTime.Now // Ended in the past
                            && !wgr.TimeRemoved.HasValue             // Or was removed from a workgroup
                        )
                    )
                    .Select(w => new WorkstationModel
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

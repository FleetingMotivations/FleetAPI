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
                        CampusName = c.CampusIdentifer
                    })
                    .ToList();

                return Ok(campuses);
            }
        }
    }
}

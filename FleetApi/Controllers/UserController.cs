using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FleetApi.Controllers
{
    public class UserController : BaseController
    {

        public IHttpActionResult GetWorkgroup(int workgroupId, int userId)
        {
            return Ok();
        }

    }
}

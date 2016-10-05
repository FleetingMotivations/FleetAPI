using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FleetApi.Results;

namespace FleetApi.Controllers
{
    public class BaseController : ApiController
    {
        protected UnprocessableEntityResult<T> Unprocessable<T>(T content)
        {
            return new UnprocessableEntityResult<T>(content, ControllerContext);
        }
    }
}

/* 
 * Description: Provides a base controller from which all FleetAPI
 * controllers extend. IMplements common functionality and resource accesss
 *
 * Project: FleetApi
 * Group Members: Jordan Collins, Tristan Newmann, Hayden Cheers, Alistair Woodcock
 * Last modified: 11/10/16
 * Last Author: Tristan Newmann
 * 
*/

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

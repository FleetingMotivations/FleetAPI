using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Newtonsoft.Json;
using ControllerContext = System.Web.Http.Controllers.HttpControllerContext;

namespace FleetApi.Results
{
    public class UnprocessableEntityResult<T> : IHttpActionResult
    {

        private T ResponseContent { get; set; }
        private ControllerContext Context { get; set; }

        public UnprocessableEntityResult(T content, ControllerContext context)
        {
            ResponseContent = content;
            Context = context;
        } 

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var message = new HttpResponseMessage {StatusCode = (HttpStatusCode) 422};
            var jsonBody = JsonConvert.SerializeObject(ResponseContent);
            message.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"); 
            return Task.FromResult(message);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Marten;
using FanyiNetwork.Models;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FanyiNetwork.Controllers
{
    [Route("api/[controller]")]
    public class NotificationController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public NotificationController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        // GET: api/values
        [HttpGet]
        public IActionResult Get()
        {
            var notifications = new List<Notification>();

            using (var session = _documentStore.LightweightSession())
            {
                notifications = session.Query<Notification>().Where(x => x.userid == int.Parse(User.FindFirst(ClaimTypes.Sid).Value) && x.read == false).OrderByDescending(x=>x.time).ToList();
            }

            return new ObjectResult(notifications);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var notifications = new List<Notification>();

            using (var session = _documentStore.LightweightSession())
            {
                notifications = session.Query<Notification>().Where(x => x.assignmentid == id && x.userid == int.Parse(User.FindFirst(ClaimTypes.Sid).Value) && x.read == false).OrderByDescending(x => x.time).ToList();
            }

            return new ObjectResult(notifications);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut]
        public IActionResult Put([FromBody]Notification model)
        {
            using (var session = _documentStore.LightweightSession())
            {
                model.read = true;
                session.Store<Notification>(model);
                session.SaveChanges();
            }
            return Ok();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

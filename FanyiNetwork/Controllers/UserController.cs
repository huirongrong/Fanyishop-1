using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Marten;
using System.Security.Claims;
using FanyiNetwork.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FanyiNetwork.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public UserController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }
        // GET: api/values
        [HttpGet]
        public IActionResult Get()
        {
            var model = new List<FanyiNetwork.Models.User>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().Where(x => x.isTerminated == false).ToList();
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();

            FanyiNetwork.Models.User model = new FanyiNetwork.Models.User();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().SingleOrDefault(x => x.id == id);
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }
        [HttpGet("group/{group}")]
        public IActionResult Get(string group)
        {
            var model = new List<FanyiNetwork.Models.User>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.User>().Where(x=>x.isTerminated == false).ToList();

                switch (group)
                {
                    case "全部编辑":
                        model = model.Where(x => x.group.IsOneOf(new string[] { "编辑部", "主编", "经理办" })).ToList();
                        break;
                    case "全部客服":
                        model = model.Where(x => x.group.IsOneOf(new string[] { "客服部", "客服主管", "经理办" })).ToList();
                        break;
                    default:
                        model = model.Where(x => x.group == group).ToList();
                        break;
                }

                
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        [HttpGet("GetLoginHistory")]
        public IActionResult GetLoginHistory(int uid)
        {
            var model = new List<FanyiNetwork.Models.LoginHistory>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.LoginHistory>().Where(x => x.userID == uid).ToList();
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public IActionResult Put([FromBody]FanyiNetwork.Models.User model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办" || x.Value == "人事部")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                session.Store<FanyiNetwork.Models.User>(model);
                session.SaveChanges();
            }

            return Ok();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [HttpGet("GetCheifStats")]
        public IActionResult GetCheifStats(int year, int month)
        {
            List<CheifStatsVM> stats = new List<CheifStatsVM>();

            using (var session = _documentStore.LightweightSession())
            {
                var editors = session.Query<User>().Where(x => x.group == "主编" && x.isTerminated == false).ToList();
                foreach (User item in editors)
                {
                    CheifStatsVM targeted = new CheifStatsVM()
                    {
                        userid = item.id,
                        name = item.name
                    };

                    var targetOrders = session.Query<Assignment>().Where(x => x.isPosted == true && x.status == "已完成" && x.chiefeditor == item.id).ToList();

                    if (year != 0) targetOrders = targetOrders.Where(x => x.addTime.ToUniversalTime().Year == year).ToList();
                    if (month != 0) targetOrders = targetOrders.Where(x => x.addTime.ToUniversalTime().Month == month).ToList();

                    foreach (Assignment assignment in targetOrders)
                    {
                        if (assignment.chiefeditor == targeted.userid)
                        {
                            targeted.totalAssign += 1;
                        }

                        if (assignment.finishDue != null && assignment.finishTime != null && assignment.reviewTime != null && assignment.reviewDue != null)
                        {
                            if (assignment.assignTime > DateTime.MinValue && assignment.assignTime.AddHours(-6).ToUniversalTime() > assignment.addTime.ToUniversalTime())
                            {
                                targeted.assignoverdue += 1;
                            }

                            if (assignment.reviewTime.First().ToUniversalTime() > ((DateTime)assignment.reviewDue).ToUniversalTime())
                            {
                                targeted.reviewoverdue += 1;
                            }
                        }

                        targeted.assignoverduePercentage = Math.Round((((double)targeted.assignoverdue / (double)targeted.totalAssign) * 100), 2);
                        targeted.reviewoverduePercentage = Math.Round((((double)targeted.reviewoverdue / (double)targeted.totalAssign) * 100), 2);

                    }

                    stats.Add(targeted);
                }
            }

            return new ObjectResult(stats);
        }

        [HttpGet("GetEditorsStats")]
        public IActionResult GetEditorsStats(int year, int month)
        {
            List<EditorStatsVM> stats = new List<EditorStatsVM>();

            using (var session = _documentStore.LightweightSession())
            {
                var editors = session.Query<User>().Where(x => x.group == "编辑部" && x.isTerminated == false).ToList();
                foreach (User item in editors)
                {
                    EditorStatsVM target = new EditorStatsVM();
                    target.userid = item.id;
                    target.name = item.name;
                    target.overdue = 0;
                    target.late = 0;

                    var targetOrders = session.Query<Assignment>().Where(x => x.isPosted == true && x.status == "已完成" && x.editor == item.id).ToList();

                    if (year != 0) targetOrders = targetOrders.Where(x => x.addTime.ToUniversalTime().Year == year).ToList();
                    if (month != 0) targetOrders = targetOrders.Where(x => x.addTime.ToUniversalTime().Month == month).ToList();

                    var reviews = targetOrders.Where(x => x.editor == target.userid && x.reviewScore > 0).ToList();
                    if (reviews != null && reviews.Count > 0)
                    {
                        target.review = Math.Round(reviews.Average(x => x.reviewScore), 2);
                    }

                    var ratings = targetOrders.Where(x => x.editor == target.userid && x.feedbackRating > 0).ToList();
                    if (ratings != null && ratings.Count > 0)
                    {
                        target.rating = Math.Round(ratings.Average(x => x.feedbackRating), 2);
                    }

                    foreach (Assignment assignment in targetOrders)
                    {
                        target.totalOrder += 1;
                        target.totalWord += assignment.wordCount;

                        if (assignment.finishDue != null && assignment.finishTime != null)
                        {
                            if (assignment.finishTime.Min().ToUniversalTime() > ((DateTime)assignment.finishDue).ToUniversalTime())
                            {
                                target.overdue += 1;
                            }

                            if (assignment.finishTime.Max().ToUniversalTime() > assignment.due.ToUniversalTime())
                            {
                                target.late += 1;
                            }
                        }

                        target.overduePercentage = Math.Round((((double)target.overdue / (double)target.totalOrder) * 100), 2);
                        target.latePercentage = Math.Round((((double)target.late / (double)target.totalOrder) * 100), 2);
                    }

                    target.efficiency = (target.totalWord / 28).ToString();

                    stats.Add(target);
                }
            }

            return new ObjectResult(stats);
        }
    }
}

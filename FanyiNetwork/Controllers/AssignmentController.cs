using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Marten;
using FanyiNetwork.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using FanyiNetwork.App_Code;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FanyiNetwork.Controllers
{
    public class AssignmentDetail {
        public Assignment AssignmentDetails { get; set; }
        public User Cs { get; set; }
        public User Editor { get; set; }
    }

    [Authorize]
    [Route("api/[controller]")]
    public class AssignmentController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public AssignmentController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        [HttpGet("getbyuser")]
        public IActionResult GetByUser(int userId, int year, int month)
        {
            List<Assignment> models;

            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);

            using (var session = _documentStore.LightweightSession())
            {
                if (userId != 0)
                {
                    models = session.Query<Assignment>().Where(x => x.isPosted == true && x.editor == userId).OrderBy(x => x.id).ToList();

                    if (year != 0) models = models.Where(x => x.addTime.ToUniversalTime().Year == year).ToList();
                    if (month != 0) models = models.Where(x => x.addTime.ToUniversalTime().Month == month).ToList();

                    return new ObjectResult(models);
                }
                else
                {
                    return Ok();
                }
            }
        }

        // GET: api/values
        [HttpGet]
        public IActionResult Get(bool onlyme, bool onlyparttime, int editor, int parttimeEditor, int cs)
        {
            List<Assignment> models;
            List<AssignmentDetail> list = new List<AssignmentDetail>();

            int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);

            using (var session = _documentStore.LightweightSession())
            {
                models = session.Query<Assignment>().Where(x => x.status != "已完成" && x.isPosted == true).ToList();

                if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办" || x.Value == "主编" || x.Value == "客服部" || x.Value == "客服主管" || x.Value == "人事部" || x.Value == "兼职编辑部")))
                {
                    models = models.Where(x => x.isParttime == false).ToList();
                }

                if (onlyparttime)
                {
                    models = models.Where(x => x.isParttime == true).ToList();
                }

                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部")))
                {
                    models = models.Where(x => x.cs == uid).ToList();
                }
                else if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "编辑部" || x.Value == "兼职编辑部")))
                {
                    models = models.Where(x => x.editor == uid).ToList();
                }

                if (onlyme)
                {
                    models = models.Where(x => x.cs == uid || x.editor == uid || x.chiefeditor == uid).ToList();
                }

                if (editor != 0)
                {
                    models = models.Where(x => x.editor == editor).ToList();
                }
                if (parttimeEditor != 0)
                {
                    models = models.Where(x => x.isParttime == true && x.editor == parttimeEditor).ToList();
                }
                if (cs != 0)
                {
                    models = models.Where(x => x.cs == cs).ToList();
                }

                models = models.OrderBy(x => x.due.ToUniversalTime()).ToList();

                foreach (Assignment item in models)
                {
                    list.Add(new AssignmentDetail()
                    {
                        AssignmentDetails = item,
                        Cs = session.Load<User>(item.cs),
                        Editor = session.Load<User>(item.editor)
                    });
                }
            }

            return new ObjectResult(list);
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetAssignment")]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();

            Assignment model = new Assignment();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<Assignment>().SingleOrDefault(x => x.id == id);
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody]Assignment model)
        {
            if (model == null) return BadRequest();

            if (String.IsNullOrEmpty(model.dueDate) || String.IsNullOrEmpty(model.dueTime))
            {
                return BadRequest("交付日期和时间不能为空!");
            }

            using (var session = _documentStore.LightweightSession())
            {
                DateTime dueDate = DateTime.Parse(model.dueDate);
                DateTime dueTime = DateTime.Parse(model.dueTime);
                DateTime finalDue = new DateTime(dueDate.Year, dueDate.Month, dueDate.Day, dueTime.Hour, dueTime.Minute, 0);
                model.due = finalDue;

                if (!model.isParttime)
                {
                    model.finishDue = CalculateFinishDue(model.wordCount, model.due);
                    model.reviewDue = CalculateReviewDue(model.wordCount, model.due);
                }
                else
                {
                    model.finishDue = finalDue;
                    model.reviewDue = finalDue;
                }

                if (model.editor != 0)
                {
                    this.Assign(model);
                }
                else
                {
                    model.status = "待派单";
                }

                model.isPosted = true;
                model.cs = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                model.addTime = DateTime.Now;

                session.Store<Assignment>(model);
                session.SaveChanges();

                if (!model.isParttime)
                {
                    var cheifs = session.Query<User>().Where(x => x.group == "主编").ToList();
                    foreach (User u in cheifs)
                    {
                        Notification notification = new Notification();
                        notification.userid = u.id;
                        notification.assignmentid = model.id;
                        notification.message = "新建单号，请及时分配!";
                        notification.time = DateTime.Now;
                        session.Store<Notification>(notification);
                    }
                }

                Flow flow = new Flow();

                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "新建了单号.";
                flow.Time = DateTime.Now;

                session.Store<Flow>(flow);
                session.SaveChanges();
            }

            return CreatedAtAction("GetAssignment", new { id = model.id }, model);
        }

        private DateTime CalculateFinishDue(int wordCount, DateTime dueTime)
        {
            if (wordCount <= 0) wordCount = 275;

            int mins = wordCount / 275 * 90;

            return dueTime.AddMinutes(-mins);
        }
        private DateTime CalculateReviewDue(int wordCount, DateTime dueTime)
        {
            if (wordCount <= 0) wordCount = 275;

            int mins = wordCount / 275 * 60;

            return dueTime.AddMinutes(-mins);
        }

        [HttpPost("assign")]
        public IActionResult Assign([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "主编" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.status = "待交稿";
                model.chiefeditor = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                model.assignTime = DateTime.Now;
                model.isParttimeConfirm = false;

                session.Store<Assignment>(model);

                Notification notification = new Notification();
                notification.userid = model.editor;
                notification.assignmentid = model.id;
                notification.message = "单号已分配至你,请按时提交稿件!";
                notification.time = DateTime.Now;
                session.Store<Notification>(notification);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "成功分配，标记单号为待交稿.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                DateTime dueTime = (DateTime)model.finishDue;
                TimeSpan ts = dueTime - DateTime.Now;
                string remainTime;
                if (ts.TotalHours >= 1)
                {
                    remainTime = ts.Days + "天," + ts.Hours + "小时," + ts.Minutes + "分钟";
                }
                else
                {
                    remainTime = "不足一小时";
                }
                
                FanyiNetwork.Models.User editor = session.Query<User>().FirstOrDefault(x => x.id == model.editor);
                if (editor != null)
                {
                    SMSNotification.SendAssignOrderSMS(model.no, remainTime, editor.mobile);
                }

                return Ok();
            }
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                if (model.due != session.Query<Assignment>().SingleOrDefault(x => x.id == model.id).due)
                {
                    model.finishDue = CalculateFinishDue(model.wordCount, model.due);
                    model.reviewDue = CalculateReviewDue(model.wordCount, model.due);
                }

                session.Store<Assignment>(model);

                Notification notification1 = new Notification();
                notification1.userid = model.editor;
                notification1.assignmentid = model.id;
                notification1.message = "单号信息已修改，请注意查看时间/字数/要求!";
                notification1.time = DateTime.Now;
                session.Store<Notification>(notification1);

                Notification notification2 = new Notification();
                notification2.userid = model.chiefeditor;
                notification2.assignmentid = model.id;
                notification2.message = "单号信息已修改，请注意查看时间/字数/要求!";
                notification2.time = DateTime.Now;
                session.Store<Notification>(notification2);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "修改了基本信息.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("approve")]
        public IActionResult Approve([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "主编"|| x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.status = "待发送";

                if (model.reviewTime == null)
                {
                    model.reviewTime = new List<DateTime>();
                }

                model.reviewTime.Add(DateTime.Now);

                session.Store<Assignment>(model);

                Notification notification1 = new Notification();
                notification1.userid = model.cs;
                notification1.assignmentid = model.id;
                notification1.message = "单号已审核完毕，请及时发送!";
                notification1.time = DateTime.Now;
                session.Store<Notification>(notification1);

                Notification notification2 = new Notification();
                notification2.userid = model.editor;
                notification2.assignmentid = model.id;
                notification2.message = "单号已审核通过，该单已完成!";
                notification2.time = DateTime.Now;
                session.Store<Notification>(notification2);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "审核通过，标记单号为待发送.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }
        [HttpPost("disapprove")]
        public IActionResult Disapprove([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "主编" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.status = "待修改";

                if (model.reviewTime == null)
                {
                    model.reviewTime = new List<DateTime>();
                }

                model.reviewTime.Add(DateTime.Now);

                session.Store<Assignment>(model);

                Notification notification1 = new Notification();
                notification1.userid = model.editor;
                notification1.assignmentid = model.id;
                notification1.message = "审核不通过，请查看主编意见及时修改!";
                notification1.time = DateTime.Now;
                session.Store<Notification>(notification1);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "审核未通过, 标记单号为待修改.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("parttimeconfirm")]
        public IActionResult ParttimeConfirm([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "兼职编辑部" || x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                if (model.isParttimeConfirm == false)
                {
                    model.status = "待派单";
                    model.editor = 0;

                    Notification notification1 = new Notification();
                    notification1.userid = model.cs;
                    notification1.assignmentid = model.id;
                    notification1.message = "兼职编辑无法接单，请重新分配单号.";
                    notification1.time = DateTime.Now;
                    session.Store<Notification>(notification1);

                    
                }

                session.Store<Assignment>(model);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("finish")]
        public IActionResult Finish([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "主编" || x.Value == "编辑部" || x.Value == "兼职编辑部" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.status = "待审核";

                if (model.finishTime == null)
                {
                    model.finishTime = new List<DateTime>();
                }

                model.finishTime.Add(DateTime.Now);

                session.Store<Assignment>(model);
                
                Notification notification1 = new Notification();
                notification1.userid = model.chiefeditor;
                notification1.assignmentid = model.id;
                notification1.message = "编辑已完成，请及时审核！";
                notification1.time = DateTime.Now;
                session.Store<Notification>(notification1);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "确认提交了稿件，标记为待审核.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("close")]
        public IActionResult Close([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.status = "已完成";

                session.Store<Assignment>(model);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "标记单号为已完成.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.isPosted = false;
                model.status = "已删除";

                session.Store<Assignment>(model);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "删除了该单.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("open")]
        public IActionResult Open([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服部" || x.Value == "客服主管" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                model.status = "待交稿";

                Notification notification1 = new Notification();
                notification1.userid = model.chiefeditor;
                notification1.assignmentid = model.id;
                notification1.message = "客服已重新开启本单!";
                notification1.time = DateTime.Now;
                session.Store<Notification>(notification1);

                Notification notification2 = new Notification();
                notification2.userid = model.editor;
                notification2.assignmentid = model.id;
                notification2.message = "客服已重新开启本单!";
                notification2.time = DateTime.Now;
                session.Store<Notification>(notification2);

                session.Store<Assignment>(model);

                Flow flow = new Flow();
                flow.AssignmentID = model.id;
                flow.Operator = User.Identity.Name;
                flow.Operation = "重启开启单号，标记单号为待交稿.";
                flow.Time = DateTime.Now;
                session.Store<Flow>(flow);

                session.SaveChanges();

                return Ok();
            }
        }

        [HttpPost("sms")]
        public IActionResult SMS([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                DateTime dueTime = (DateTime)model.finishDue;
                TimeSpan ts = dueTime - DateTime.Now;
                string remainTime;
                if (ts.TotalHours >= 1)
                {
                    remainTime = ts.Days + "天," + ts.Hours + "小时," + ts.Minutes + "分钟";
                }
                else
                {
                    remainTime = "不足一小时";
                }

                FanyiNetwork.Models.User editor = session.Query<User>().FirstOrDefault(x => x.id == model.editor);
                if (editor != null)
                {
                    SMSNotification.SendChangeOrderSMS(model.no, remainTime, editor.mobile);
                }

                return Ok();
            }
        }

        [HttpPost("feedback")]
        public IActionResult feedback([FromBody]Assignment model)
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办" || x.Value == "人事部")))
            {
                return BadRequest("你没有权限执行该操作!");
            }

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                session.Store<Assignment>(model);
                session.SaveChanges();
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("rating")]
        public IActionResult Rating([FromBody]Assignment model)
        {
            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                var original = session.Query<Assignment>().FirstOrDefault(x => x.id == model.id);

                original.feedbackRating = model.feedbackRating;
                original.feedbackContent = model.feedbackContent;

                session.Store<Assignment>(model);
                session.SaveChanges();

                return Ok();
            }
        }
    }
}

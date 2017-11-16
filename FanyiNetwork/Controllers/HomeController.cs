using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Marten;
using FanyiNetwork.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FanyiNetwork.App_Code;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net;

namespace FanyiNetwork.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public HomeController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        [HttpGet]
        [Authorize]
        public IActionResult AllAssignment()
        {
            ViewBag.Title = "调试页面 - 凡易单号管理系统";
            ViewData["active-name"] = "5";

            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办")))
            {
                return RedirectToAction("Index");
            }

            List<Assignment> assignments = new List<Models.Assignment>();

            using (var session = _documentStore.LightweightSession())
            {
                assignments = session.Query<Assignment>().ToList();
            }

            return new ObjectResult(assignments);
        }

        [Authorize]
        public IActionResult IntegrityCheck()
        {
            ViewBag.Title = "完整性检查 - 凡易单号管理系统";

            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办")))
            {
                return RedirectToAction("Index");
            }


            List<User> users = new List<Models.User>();


            List<Assignment> assignments = new List<Models.Assignment>();

            using (var session = _documentStore.LightweightSession())
            {
                users = session.Query<User>().ToList();

                foreach (User item in users)
                {
                    if (item.group == "编辑部")
                    {
                        item.password = "fanyi456456";
                    }

                    if (item.group == "主编")
                    {
                        item.password = "fanyi456456";
                    }

                    if (item.account == "teakobe")
                    {
                        item.password = "teakobe19860529";
                    }

                    session.Store<User>(item);
                }

                assignments = session.Query<Assignment>().ToList();

                foreach (Assignment item in assignments)
                {
                    if (item.isPosted == false)
                    {
                        session.Delete<Assignment>(item);
                    }
                    else
                    {
                        //检查reviewScores
                        if (item.reviewScores == null) item.reviewScores = new List<int>();

                        //检查addTime
                        if (item.addTime == null || item.addTime <= DateTime.MinValue)
                        {
                            var target = session.Query<Flow>().Where(x => x.AssignmentID == item.id && x.Operation == "新建了单号.").FirstOrDefault();
                            if (target != null)
                            {
                                item.addTime = target.Time;
                            }
                        }

                        //检查finishDue & reviewDue
                        item.finishDue = CalculateFinishDue(item.wordCount, item.due);
                        item.reviewDue = CalculateReviewDue(item.wordCount, item.due);
                        
                        //检查assignTime
                        if (item.assignTime == null || item.assignTime <= DateTime.MinValue)
                        {
                            var target = session.Query<Flow>().Where(x => x.AssignmentID == item.id && x.Operation == "成功分配，标记单号为待交稿.").OrderByDescending(x=>x.Time).FirstOrDefault();
                            if (target != null)
                            {
                                item.assignTime = target.Time;
                            }
                            else
                            {
                                item.assignTime = item.addTime;
                            }
                        }

                        if (item.status == "已完成")
                        {
                            //检查finishTime
                            if (item.finishTime == null)
                            {
                                var targets = session.Query<Flow>().Where(x => x.AssignmentID == item.id && (x.Operation == "确认提交了稿件，标记为待审核." || x.Operation == "确认提交了稿件，标记为待审核")).ToList();
                                if (targets.Count > 0)
                                {
                                    item.finishTime = new List<DateTime>();
                                    foreach (Flow f in targets)
                                    {
                                        item.finishTime.Add(f.Time);
                                    }
                                }
                            }
                            //检查reviewTime
                            if (item.reviewTime == null)
                            {
                                var targets = session.Query<Flow>().Where(x => x.AssignmentID == item.id && x.Operation == "审核通过，标记单号为待发送.").ToList();
                                if (targets.Count > 0)
                                {
                                    item.reviewTime = new List<DateTime>();
                                    foreach (Flow f in targets)
                                    {
                                        item.reviewTime.Add(f.Time);
                                    }
                                }
                            }
                        }

                        session.Store<Assignment>(item);
                    }
                }

                session.SaveChanges();
            }

            return View(assignments);
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

        [Authorize]
        public IActionResult Index()
        {
            ViewBag.Title = "总览 - 凡易单号管理系统";
            ViewData["active-name"] = "1";

            return View();
        }

        [Authorize]
        public IActionResult People()
        {
            ViewBag.Title = "人员 - 凡易单号管理系统";
            ViewData["active-name"] = "2";

            return View();
        }

        [Authorize]
        public IActionResult IP()
        {
            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办")))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Title = "人员 - 凡易单号管理系统";

            return View();
        }

        [Authorize]
        public IActionResult Stats()
        {
            ViewBag.Title = "统计 - 凡易单号管理系统";
            ViewData["active-name"] = "5";

            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "经理办" || x.Value == "人事部" || x.Value == "主编")))
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [Authorize]
        public IActionResult AssignmentList(int userId)
        {
            ViewBag.Title = "全部单号 - 凡易单号管理系统";
            ViewData["active-name"] = "5";

            if (userId != 0)
            {
                ViewData["userId"] = userId;
                using (var session = _documentStore.LightweightSession())
                {
                    ViewData["userName"] = session.Query<User>().FirstOrDefault(x=>x.id == userId).name;
                }
                
            }
            else
            {
                ViewData["userId"] = User.FindFirst(ClaimTypes.Sid).Value;
                ViewData["userName"] = User.Identity.Name;
            }

            return View();
        }
        
        [Authorize]
        [HttpGet]
        public string ReleaseUrl(int id)
        {
            string ecripted = AESEncode.Encrypt(id.ToString());

            string original = UriHelper.GetDisplayUrl(Request);
            original = original.Replace("ReleaseUrl", "Release").Replace(id.ToString(), "");
            original = original.Substring(0, original.Length - 1);

            return original + "/" + WebUtility.HtmlEncode(ecripted);
        }

        public IActionResult Release(string id)
        {
            if (id == null)
            {
                return View();
            }

            try
            {
                ViewBag.Title = "下载页面";

                int assignmentId = int.Parse(AESEncode.Decrypt(WebUtility.HtmlDecode(id)));

                using (var session = _documentStore.LightweightSession())
                {
                    Assignment model = session.Query<Assignment>().SingleOrDefault(x => x.id == assignmentId);

                    FanyiNetwork.Models.User cs = session.Query<User>().SingleOrDefault(x => x.id == model.cs);
                    ViewData["wechat"] = cs.wechat;
                    ViewData["wechat_img"] = cs.barcode;
                    ViewData["qq"] = cs.qq;

                    return View(model);
                }
            }
            catch {
                return View();
            }
        }

        [Authorize]
        public IActionResult Assignment(string id)
        {
            if (id == null) return RedirectToAction("Index");

            Assignment model = new Assignment();
            List<FanyiNetwork.Models.User> editors = new List<Models.User>();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<Assignment>().SingleOrDefault(x => x.no == id);

                if (model == null) return RedirectToAction("Index");

                //兼职编辑无法查看其它单号
                if (User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "兼职编辑部")) && model.editor != int.Parse(User.FindFirst(ClaimTypes.Sid).Value))
                {
                    return RedirectToAction("Index");
                }

                ViewBag.Title =  model.no + " - 凡易单号管理系统";

                ViewData["Cs"] = session.Query<User>().SingleOrDefault(x => x.id == model.cs).name;

                if (model.isParttime)
                {
                    ViewData["Editors"] = session.Query<User>().Where(x => x.isTerminated == false && x.group == "兼职编辑部").ToList();
                }
                else
                {
                    ViewData["Editors"] = session.Query<User>().Where(x => x.isTerminated == false && x.group.IsOneOf(new string[] { "主编", "编辑部", "经理办" })).ToList();
                }

                string ecripted = AESEncode.Encrypt(model.id.ToString());
                ViewData["encodedUrl"] = WebUtility.HtmlEncode(ecripted);
            }

            return View(model);
        }

        [Authorize]
        public IActionResult Add()
        {
            ViewBag.Title = "新建单号 - 凡易单号管理系统";

            ViewData["active-name"] = "3";

            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "经理办")))
            {
                return RedirectToAction("Index");
            }

            Assignment model = new Assignment();

            using (var session = _documentStore.LightweightSession())
            {
                string todayStarting = DateTime.Now.ToString("MMddyy");

                var todayOrders = session.Query<Assignment>().Where(x => x.no.StartsWith(todayStarting)).Count();

                todayOrders += 1;
                model.no = todayStarting + todayOrders.ToString("000");

                while (session.Query<Assignment>().SingleOrDefault(x => x.no == model.no) != null)
                {
                    todayOrders += 1;
                    model.no = todayStarting + todayOrders.ToString("000");
                }

                model.status = "未创建";
                model.isPosted = false;
                model.isParttime = false;

                session.Store<Assignment>(model);
                session.SaveChanges();
            }

            return View(model);
        }

        [Authorize]
        public IActionResult Add2()
        {
            ViewBag.Title = "新建兼职单号 - 凡易单号管理系统";

            ViewData["active-name"] = "4";

            if (!User.HasClaim(x => x.Type == ClaimTypes.Role && (x.Value == "客服主管" || x.Value == "客服部" || x.Value == "人事部" || x.Value == "经理办")))
            {
                return RedirectToAction("Index");
            }

            Assignment model = new Assignment();

            using (var session = _documentStore.LightweightSession())
            {
                string todayStarting = DateTime.Now.ToString("MMddyy");

                var todayOrders = session.Query<Assignment>().Where(x => x.no.StartsWith(todayStarting)).Count();

                todayOrders += 1;
                model.no = todayStarting + todayOrders.ToString("000");

                while (session.Query<Assignment>().SingleOrDefault(x => x.no == model.no) != null)
                {
                    todayOrders += 1;
                    model.no = todayStarting + todayOrders.ToString("000");
                }

                model.status = "未创建";
                model.isPosted = false;
                model.isParttime = true;

                session.Store<Assignment>(model);
                session.SaveChanges();
            }

            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

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
    [Authorize]
    public class ClientController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public ClientController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "总览 - 凡易单号管理系统";
            ViewData["active-name"] = "6";

            return View();
        }

        // POST api/values
        [HttpPost]
        [Route("api/[controller]/add")]
        public IActionResult Post([FromBody]Client model)
        {

            if (model == null) return BadRequest();

            using (var session = _documentStore.LightweightSession())
            {
                if (session.Query<Client>().SingleOrDefault(x => x.name == model.name) != null)
                {
                    return BadRequest("该客户昵称已存在！");
                }

                session.Store<Client>(model);
                session.SaveChanges();
            }

            return Ok();
        }
        /// <summary>
        /// 根据昵称检索
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>

        [HttpGet]
        [Route("api/[controller]")]
        public IActionResult Get(string nick)
        {
            List<Client> models;

            using (var session = _documentStore.LightweightSession())
            {
                var data = session.Query<Client>();
                if (nick != null && nick != "")
                    data = data.Where(x => x.name.Contains(nick)) as Marten.Linq.IMartenQueryable<Client>;
                models = data.ToList();
                return new ObjectResult(models);
            }
        }

        [HttpGet]
        [Route("api/[controller]/{id}")]
        public IActionResult Get(int id)
        {
            if (id == 0) return BadRequest();

            FanyiNetwork.Models.Client model = new FanyiNetwork.Models.Client();

            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.Client>().SingleOrDefault(x => x.id == id);
            }

            if (model == null) return BadRequest();

            return new ObjectResult(model);
        }

        /// <summary>
        /// 返回编辑页面 
        /// </summary>
        /// <returns></returns>
        public IActionResult Edit()
        {
            return View();
        }
        /// <summary>
        /// 保存编辑数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/[controller]/edit")]
        public IActionResult SaveEdit([FromBody] Client model)
        {
            if (model == null) return BadRequest();
            using (var session = _documentStore.LightweightSession())
            {
                var data = session.Query<Client>();
                session.Store<Client>(model);
                session.SaveChanges();
            }
            return Ok("保存成功");
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/[controller]/del")]
        public IActionResult Del(int id)
        {
            if (id == 0) return BadRequest();
            FanyiNetwork.Models.Client model = new FanyiNetwork.Models.Client();
            using (var session = _documentStore.LightweightSession())
            {
                model = session.Query<FanyiNetwork.Models.Client>().SingleOrDefault(x => x.id == id);
                session.Delete<Client>(model);
                session.SaveChanges();
            }
            return Ok("删除成功");
        }
    }
}

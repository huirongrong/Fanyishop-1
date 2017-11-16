using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Marten;
using FanyiNetwork.Models;

namespace FanyiNetwork.Controllers
{
    public class UploadFile {
        public string name { get; set; }
        public string size { get; set; }
        public DateTime time { get; set; }
    }

    public class FileController : Controller
    {
        private IHostingEnvironment _environment;
        private readonly IDocumentStore _documentStore;
        public FileController(IHostingEnvironment environment, IDocumentStore documentStore)
        {
            _environment = environment;
            _documentStore = documentStore;
        }

        [HttpGet]
        public IActionResult FetchFiles(string no, string type)
        {
            var filePath = CheckDirectory(no, type);

            List<FileInfo> folder = GetFilesByDir(filePath);

            if (folder == null || folder.Count <= 0) return Ok();

            List<UploadFile> result = new List<UploadFile>();
            foreach (FileInfo fi in folder)
            {
                result.Add(new UploadFile() {
                    name = fi.Name,
                    size = (fi.Length / 1024).ToString() + "KB",
                    time = fi.CreationTime
                });
            }

            return new ObjectResult(result.OrderByDescending(x=>x.time).ToList());
        }

        [HttpGet]
        public IActionResult FinishWork(string no, string fileName)
        {
            var filePath = CheckDirectory(no, "works");
            List<FileInfo> folder = GetFilesByDir(filePath);

            try
            {
                FileInfo originalFile = folder.SingleOrDefault(x => x.Name == fileName);
                if (originalFile != null)
                {
                    originalFile.CopyTo(CheckDirectory(no, "finished") + "\\" + fileName, true);
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch
            {
                return BadRequest();
            }
        }
        [HttpGet]
        public IActionResult DeleteWork(string no, string fileName)
        {
            var filePath = CheckDirectory(no, "finished");
            List<FileInfo> folder = GetFilesByDir(filePath);

            try
            {
                FileInfo originalFile = folder.SingleOrDefault(x => x.Name == fileName);
                if (originalFile != null)
                {
                    originalFile.Delete();
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
                
            }
            catch
            {
                return BadRequest();
            }
        }

        public List<FileInfo> GetFilesByDir(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);

            //找到该目录下的文件  
            FileInfo[] fi = di.GetFiles();

            if (fi == null) return null;

            //把FileInfo[]数组转换为List  
            List<FileInfo> list = fi.ToList<FileInfo>();
            return list;
        }

        [HttpPost]
        public async Task<IActionResult> Post(IFormCollection collection, string no, string type)
        {
            var files = collection.Files;

            long size = files.Sum(f => f.Length);

            var filePath = CheckDirectory(no, type);

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    try
                    {
                        using (var stream = new FileStream(Path.Combine(filePath, formFile.FileName), FileMode.CreateNew))
                        {
                            await formFile.CopyToAsync(stream);
                        }
                    }
                    catch (IOException e)
                    {
                        return BadRequest("该文件已存在！请重命名后重新上传");
                    }

                    string typeOfFile;

                    switch (type)
                    {
                        case "attachments":
                            typeOfFile = "附件";
                            break;
                        case "works":
                            typeOfFile = "稿件";
                            break;
                        default:
                            typeOfFile = String.Empty;
                            break;
                    }

                    using (var session = _documentStore.LightweightSession())
                    {
                        int assignmentID = session.Query<Assignment>().SingleOrDefault(x => x.no == no).id;

                        Flow model = new Flow();

                        model.AssignmentID = assignmentID;
                        model.Operator = User.Identity.Name;
                        model.Operation = "上传了" + typeOfFile + formFile.FileName;
                        model.Time = DateTime.Now;

                        session.Store<Flow>(model);
                        session.SaveChanges();
                    }
                }
            }

            return Ok(new { count = files.Count, size, filePath });
        }

        private string CheckDirectory(string no, string type)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            filePath = Path.Combine(filePath, no);
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            var attachmentPath = Path.Combine(filePath, type);
            if (!Directory.Exists(attachmentPath)) Directory.CreateDirectory(attachmentPath);

            return attachmentPath;
        }
    }
}

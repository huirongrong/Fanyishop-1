using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FanyiNetwork.Models
{
    public class BaseModel
    {
        public int id { get; set; }
    }

    public class User : BaseModel
    {
        public string account { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        public string qq { get; set; }
        public string mobile { get; set; }
        public string info { get; set; }
        public string group { get; set; }
        public bool status { get; set; }
        public string wechat { get; set;}
        public string barcode { get; set; }

        public bool isTerminated { get; set; }
    }

    public class LoginHistory : BaseModel
    {
        public int userID { get; set; }
        public string ipAddress { get; set; }
        public DateTime addTime { get; set; }
    }

    public class Client : BaseModel {
        public string name { get; set; }
        public string realName { get; set; }
        public string sex { get; set; }
        public DateTime birthday { get; set; }
        public string memo { get; set; }
        public DateTime addTime { get; set; }
    }

    public class Assignment : BaseModel
    {
        public string no { get; set; }
        public string status { get; set; }
        public bool isPosted { get; set; }
        public bool isParttime { get; set; }
        public bool isParttimeConfirm { get; set; }

        //基本信息
        public int client { get; set; }
        public string topic { get; set; }
        public string style { get; set; }
        public int referenceCount { get; set; }
        public bool isImportant { get; set; }
        public string other { get; set; }

        public int wordCount { get; set; }
        public int pageCount { get; set; }

        //负责人
        public int cs { get; set; }
        public int chiefeditor { get; set; }
        public int editor { get; set; }

        public string currency { get; set; }
        public string cost { get; set; }

        //标签
        public List<string> Labels { get; set; }

        //时间信息
        public string dueDate { get; set; }
        public string dueTime { get; set; }
        public DateTime due { get; set; }
        public DateTime addTime { get; set; }
        public DateTime assignTime { get; set; }

        public object finishDue { get; set; }
        public object reviewDue { get; set; }

        public List<DateTime> finishTime { get; set; }
        public List<DateTime> reviewTime { get; set; }

        public List<string> reviews { get; set; }

        public List<int> reviewScores { get; set; }
        public int reviewScore { get; set; }

        public int feedbackCount { get; set; }
        public int feedbackRating { get; set; }
        public string feedbackContent { get; set; }

        public bool isClass { get; set; }

        //反馈
        public bool Late { get; set; }
        public bool Plagiarism { get; set; }
        public bool NegativeReview { get; set; }
        public bool Fail { get; set; }
        public bool Refund { get; set; }

        /// <summary>
        /// 留言
        /// </summary>
        public List<string> Messages { get; set; }
    }

    public class Flow : BaseModel
    {
        public int AssignmentID { get; set; }
        public string Operator { get; set; }
        public string Operation { get; set; }
        public DateTime Time { get; set; }
    }

    public class Notification : BaseModel
    {
        public int userid { get; set; }
        public int assignmentid { get; set; }
        public string message { get; set; }
        public bool read { get; set; }
        public DateTime time { get; set; }
    }
}

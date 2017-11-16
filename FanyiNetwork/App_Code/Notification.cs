using System.IO;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System;

namespace FanyiNetwork.App_Code
{
    public class SMSNotification
    {
        public static async void SendChangeOrderSMS(string no, string timeRemain, string mobile)
        {
            string host = "http://sms.market.alicloudapi.com";
            string path = "/singleSendSms";
            string method = "GET";
            string appcode = "8f4ea6450e6f4d7b9b0c417f690b7647";

            string param = WebUtility.UrlEncode("{\"no\":\"" + no + "\",\"time\":\"" + timeRemain + "\"}");

            string querys = "ParamString=" + param + "&RecNum=" + mobile + "&SignName=" + WebUtility.UrlEncode("凡易单号管理系统") + "&TemplateCode=SMS_75775137";
            string bodys = "";
            string url = host + path;
            WebRequest httpRequest = null;
            WebResponse httpResponse = null;

            if (0 < querys.Length)
            {
                url = url + "?" + querys;
            }

            if (host.Contains("https://"))
            {
                httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(url));
            }
            else
            {
                httpRequest = (HttpWebRequest)WebRequest.Create(url);
            }
            httpRequest.Method = method;
            httpRequest.Headers["Authorization"] = "APPCODE " + appcode;
            httpRequest.ContentType = "application/json; charset=utf-8";

            if (0 < bodys.Length)
            {
                byte[] data = Encoding.UTF8.GetBytes(bodys);
                Stream stream = await httpRequest.GetRequestStreamAsync();
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            try
            {
                httpResponse = await httpRequest.GetResponseAsync();
            }
            catch (Exception ex)
            {
                string s = ex.InnerException.ToString();
            }

        }

        public static async void SendAssignOrderSMS(string no, string timeRemain, string mobile)
        {
            string host = "http://sms.market.alicloudapi.com";
            string path = "/singleSendSms";
            string method = "GET";
            string appcode = "8f4ea6450e6f4d7b9b0c417f690b7647";

            string param = WebUtility.UrlEncode("{\"no\":\"" + no + "\",\"time\":\"" + timeRemain + "\"}");

            string querys = "ParamString=" + param + "&RecNum=" + mobile + "&SignName=" + WebUtility.UrlEncode("凡易单号管理系统") + "&TemplateCode=SMS_75840130";
            string bodys = "";
            string url = host + path;
            WebRequest httpRequest = null;
            WebResponse httpResponse = null;

            if (0 < querys.Length)
            {
                url = url + "?" + querys;
            }

            if (host.Contains("https://"))
            {
                httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(url));
            }
            else
            {
                httpRequest = (HttpWebRequest)WebRequest.Create(url);
            }
            httpRequest.Method = method;
            httpRequest.Headers["Authorization"] = "APPCODE " + appcode;
            httpRequest.ContentType = "application/json; charset=utf-8";

            if (0 < bodys.Length)
            {
                byte[] data = Encoding.UTF8.GetBytes(bodys);
                Stream stream = await httpRequest.GetRequestStreamAsync();
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            try
            {
                httpResponse = await httpRequest.GetResponseAsync();
            }
            catch (Exception ex)
            {
                string s = ex.InnerException.ToString();
            }
            
        }

        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
    }
}

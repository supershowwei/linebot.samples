using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using RestSharp;

namespace LineBotSamples.Controllers
{
    public class ChatController : Controller
    {
        private XDocument xdoc;

        // GET: Chat
        public ActionResult Index()
        {
            ViewBag.ReceivedMessage = "";

            if (System.IO.File.Exists(Path.Combine(Server.MapPath("~/"), "receivedMessage.txt")))
            {
                using (StreamReader sr = new StreamReader(Path.Combine(Server.MapPath("~/"), "receivedMessage.txt")))
                {
                    ViewBag.ReceivedMessage = sr.ReadToEnd();
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult Receive()
        {
            string rawData = "";

            Request.InputStream.Seek(0, SeekOrigin.Begin);
            using (StreamReader sr = new StreamReader(Request.InputStream))
            {
                rawData = sr.ReadToEnd();
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(Server.MapPath("~/"), "receivedMessage.txt")))
            {
                sw.Write(rawData);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public ActionResult Send(string message)
        {
            this.xdoc = XDocument.Load(Path.Combine(Server.MapPath("~/"), "confidential.xml"));

            RestClient restClient = new RestClient(@"https://trialbot-api.line.me/v1/events");
            RestRequest restRequest = new RestRequest(Method.POST);

            restRequest.AddHeader("Content-Type", "application/json; charser=UTF-8");
            restRequest.AddHeader("X-Line-ChannelID", this.xdoc.Root.Element("ChannelID").Value);
            restRequest.AddHeader("X-Line-ChannelSecret", this.xdoc.Root.Element("ChannelSecret").Value);
            restRequest.AddHeader("X-Line-Trusted-User-With-ACL", this.xdoc.Root.Element("ChannelMID").Value);

            restRequest.AddJsonBody(new
            {
                to = new[] { "u5912407b444e54885d00111f7b0ce375" },
                toChannel = 1383378250,
                eventType = "138311608800106203",
                content = new
                {
                    contentType = 1,
                    toType = 1,
                    text = message
                }
            });

            var restResponse = restClient.Execute(restRequest);

            return View("Index");
        }
    }
}
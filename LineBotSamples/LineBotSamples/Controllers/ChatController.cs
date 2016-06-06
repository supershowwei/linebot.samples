using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using Jil;
using LineBotSamples.SignalR;
using Microsoft.AspNet.SignalR;
using RestSharp;

namespace LineBotSamples.Controllers
{
    public class ChatController : Controller
    {
        // GET: Chat
        public ActionResult Index()
        {
            return this.View();
        }

        [HttpPost]
        public ActionResult Receive()
        {
            XDocument xml = XDocument.Load(Server.MapPath("~/App_Data/customers.xml"));

            string rawData = string.Empty;

            Request.InputStream.Seek(0, SeekOrigin.Begin);
            using (StreamReader sr = new StreamReader(Request.InputStream))
            {
                rawData = sr.ReadToEnd();
            }

            var results = JSON.DeserializeDynamic(rawData).result;

            string customerId = results[0].content.from;
            string text = results[0].content.text;

            var customer =
                xml.Root.Elements()
                    .Where(c => c.Attribute("Id").Value.Equals(customerId))
                    .SingleOrDefault();

            if (customer == null)
            {
                // 客戶不存在就建立客戶資料
                customer = this.CreateCustomer(customerId, xml);

                this.SendMessage("客人您好，請問該怎麼稱呼您呢？", customer.Attribute("Id").Value);
            }
            else if (customer.Attribute("FlowId").Value.Equals("0"))
            {
                // 儲存客戶稱呼
                customer.Attribute("Name").Value = results[0].content.text;
                customer.Attribute("FlowId").Value = "1";

                this.SaveCustomers(xml);

                this.SendMessage(
                    $"{customer.Attribute("Name").Value}，您好！請問您住哪裡？",
                    customer.Attribute("Id").Value);
            }
            else if (customer.Attribute("FlowId").Value.Equals("1"))
            {
                // 儲存客戶居住地
                customer.Attribute("Place").Value = results[0].content.text;
                customer.Attribute("FlowId").Value = "2";

                this.SaveCustomers(xml);

                this.SendMessage(
                    $"{customer.Attribute("Place").Value}的{customer.Attribute("Name").Value}，您好！請問有什麼可以為您服務的嗎？",
                    customer.Attribute("Id").Value);

                // 客服人員接起
                var customerServiceHub = GlobalHost.ConnectionManager.GetHubContext<CustomerServiceHub>();

                customerServiceHub.Clients.All.inbound(
                    $"{customer.Attribute("Place").Value}的{customer.Attribute("Name").Value}來了，正在線上。",
                    customer.Attribute("Id").Value);
            }
            else if (customer.Attribute("FlowId").Value.Equals("2"))
            {
                var customerServiceHub = GlobalHost.ConnectionManager.GetHubContext<CustomerServiceHub>();

                if (text.StartsWith("再見", StringComparison.Ordinal))
                {
                    // 客戶道再見
                    customer.Attribute("FlowId").Value = "0";

                    this.SaveCustomers(xml);

                    customerServiceHub.Clients.All.chat(
                        "客戶離開！",
                        customer.Attribute("Id").Value,
                        customer.Attribute("Place").Value,
                        customer.Attribute("Name").Value);
                }
                else
                {
                    // 客服人員與客戶溝通
                    customerServiceHub.Clients.All.chat(
                        text,
                        customer.Attribute("Id").Value,
                        customer.Attribute("Place").Value,
                        customer.Attribute("Name").Value);
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public ActionResult Send(string message, string customerId)
        {
            this.SendMessage(message, customerId);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private XElement CreateCustomer(string id, XDocument xml)
        {
            XElement customer =
                new XElement(
                    "Customer",
                    new XAttribute("Id", id),
                    new XAttribute("Name", string.Empty),
                    new XAttribute("Place", string.Empty),
                    new XAttribute("FlowId", 0));

            xml.Root.Add(customer);

            this.SaveCustomers(xml);

            return customer;
        }

        private void SendMessage(string message, string customerId)
        {
            XDocument xml = XDocument.Load(Server.MapPath("~/App_Data/confidential.xml"));

            RestClient restClient = new RestClient(@"https://trialbot-api.line.me/v1/events");
            RestRequest restRequest = new RestRequest(Method.POST);

            restRequest.AddHeader("Content-Type", "application/json; charser=UTF-8");
            restRequest.AddHeader("X-Line-ChannelID", xml.Root.Element("ChannelID").Value);
            restRequest.AddHeader("X-Line-ChannelSecret", xml.Root.Element("ChannelSecret").Value);
            restRequest.AddHeader("X-Line-Trusted-User-With-ACL", xml.Root.Element("ChannelMID").Value);

            restRequest.AddJsonBody(new
            {
                to = new[] { customerId },
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
        }

        private void SaveCustomers(XDocument xml)
        {
            xml.Save(Server.MapPath("~/App_Data/customers.xml"));
        }
    }
}
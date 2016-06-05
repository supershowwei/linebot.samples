using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace LineBotSamples.SignalR
{
    public class CustomerServiceHub : Hub
    {
        public void Chat(string message, string customerId, string customerName, string customerPlace)
        {
            Clients.All.chat(message, customerId, customerName, customerPlace);
        }
    }
}
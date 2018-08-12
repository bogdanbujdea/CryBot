using Microsoft.AspNetCore.SignalR;

using System;
using System.Threading.Tasks;

namespace CryBot.Core.Notifications
{
    public class ApplicationHub: Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected");
            return base.OnConnectedAsync();
        }
    }    
}

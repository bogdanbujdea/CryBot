using Microsoft.AspNetCore.SignalR;

using System;
using System.Threading.Tasks;

namespace CryBot.Core.Hubs
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

using Microsoft.AspNetCore.SignalR;

namespace _sever.MyHub
{
    public class MyHub : Hub
    {
        public async Task AddConnectionToGroup(string table, string nickname, string avatarUrl)
        {
             Console.WriteLine(table+"调用成功");
             await this.Groups.AddToGroupAsync(this.Context.ConnectionId, table);
             await this.Clients.Client(this.Context.ConnectionId).SendAsync("sameTableCustomer", nickname, avatarUrl);
        }
        public async Task NotifySameTableCustomer(string tableNo)
        {
            //await this.Clients.Groups(tableNo).SendAsync("IsRefreshOrderDetails",true);
            await this.Clients.GroupExcept(tableNo, new List<string> { this.Context.ConnectionId }).SendAsync("IsRefreshOrderDetails", true);
        }
        public async Task RefreshOrderState(bool state)
        {
            Console.WriteLine(state);
            //await this.Clients.Client(this.Context.ConnectionId).SendAsync("refreshOrderState", state);
            await this.Clients.All.SendAsync("refreshOrderState", state);
            
        }
    }
}

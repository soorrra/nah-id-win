using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Petsitter.Models;
using System;
using System.Threading.Tasks;

namespace Petsitter.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly PetsitterContext _db;

        public ChatHub(PetsitterContext db)
        {
            _db = db;
        }

        public async Task SendMessage( string message, int fromUserID, int toUserID)
        {
            var userName = Context.User.Identity.Name;
            await Clients.All.SendAsync("ReceiveMessage", userName, message);

            // Сохранение сообщения в базу данных
            var newMessage = new Message
            {
                chatID = 1, // Здесь нужно указать соответствующий chatID
                fromUserID = fromUserID, // Здесь нужно указать соответствующий fromUserID
                toUserID = toUserID, // Здесь нужно указать соответствующий toUserID
                messageText = message,
                timestamp = DateTime.UtcNow
            };

            _db.Messages.Add(newMessage);
            await _db.SaveChangesAsync();
        }
    }
}

















































































//   ＼(＾▽＾)／
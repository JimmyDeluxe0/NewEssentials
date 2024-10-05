using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;

namespace NewEssentials.Chat
{
    public class UserDisconnectedListener : IEventListener<UnturnedUserDisconnectedEvent>
    {
        private readonly IConfiguration m_Configuration;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly UnturnedUserDirectory m_UnturnedUserDirectory;
        private readonly bool m_LeaveMessages;

        public UserDisconnectedListener(IConfiguration configuration,
            IStringLocalizer stringLocalizer,
            UnturnedUserDirectory unturnedUserDirectory)
        {
            m_Configuration = configuration;
            m_StringLocalizer = stringLocalizer;
            m_UnturnedUserDirectory = unturnedUserDirectory;

            m_LeaveMessages = m_Configuration.GetValue<bool>("connections:leaveMessages");
        }

        public async Task HandleEventAsync(object? sender, UnturnedUserDisconnectedEvent @event)
        {
            if (!m_LeaveMessages)
                return;

            string userName = @event.User.DisplayName;

            Parallel.ForEach(m_UnturnedUserDirectory.GetOnlineUsers(),
                x => x.PrintMessageAsync(m_StringLocalizer["connections:disconnected", new { User = userName }]));
        }
    }
}

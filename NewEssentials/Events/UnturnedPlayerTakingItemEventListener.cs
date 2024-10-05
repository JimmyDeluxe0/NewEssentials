using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using OpenMod.Unturned.Players.Inventory.Events;

namespace AdvancedRestrictor.Events
{
    public class EventListener : IEventListener<UnturnedPlayerTakingItemEvent>
    {
        private readonly IConfiguration m_Configuration;
        private readonly IUserManager m_UserManager;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly IStringLocalizer m_StringLocalizer;

        public EventListener(IConfiguration configuration, IUserManager userManager, IPermissionChecker permissionChecker, IStringLocalizer stringLocalizer)
        {
            m_Configuration = configuration;
            m_UserManager = userManager;
            m_PermissionChecker = permissionChecker;
            m_StringLocalizer = stringLocalizer;
        }

        public async Task HandleEventAsync(object sender, UnturnedPlayerTakingItemEvent @event)
        {
            var find = m_Configuration.GetSection("restrictions:items").Get<List<Restriction>>().FirstOrDefault(x => x.ids != null && x.ids.Contains(@event.ItemData.item.id));

            if (find != null && find.bypassPermission != null)
            {
                var user = await m_UserManager.FindUserAsync(KnownActorTypes.Player, @event.Player.SteamId.ToString(), UserSearchMode.FindById);
                var check = await m_PermissionChecker.CheckPermissionAsync(user!, find.bypassPermission);

                if (check == PermissionGrantResult.Grant) return;

                @event.IsCancelled = true;
                await @event.Player.PrintMessageAsync(m_StringLocalizer["restrictions:pickup_warning"], Color.IndianRed);
            }
        }
    }
}

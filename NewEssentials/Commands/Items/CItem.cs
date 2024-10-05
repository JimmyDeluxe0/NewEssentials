using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using NewEssentials.Extensions;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Users;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NewEssentials.Commands.Items
{
    [Command("item")]
    [CommandAlias("i")]
    [CommandDescription("Spawn an item")]
    [CommandSyntax("<id>/<item name> [amount]")]
    [CommandActor(typeof(UnturnedUser))]
    public class CItem : UnturnedCommand
    {
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IConfiguration m_Configuration;
        private readonly IItemDirectory m_ItemDirectory;
        private readonly IUserManager m_UserManager;
        private readonly IPermissionChecker m_PermissionChecker;

        public CItem(IStringLocalizer stringLocalizer, IServiceProvider serviceProvider, IConfiguration configuration,
            IItemDirectory itemDirectory, IUserManager userManager, IPermissionChecker permissionChecker) : base(serviceProvider)
        {
            m_StringLocalizer = stringLocalizer;
            m_Configuration = configuration;
            m_ItemDirectory = itemDirectory;
            m_UserManager = userManager;
            m_PermissionChecker = permissionChecker;
        }

        protected override async UniTask OnExecuteAsync()
        {
            // User either didn't provide an item or provided too much information
            if (Context.Parameters.Length < 1 || Context.Parameters.Length > 2)
                throw new CommandWrongUsageException(Context);

            string rawInput = Context.Parameters[0];
            var item = await m_ItemDirectory.FindByNameOrIdAsync(rawInput);

            if (item == null)
                throw new CommandWrongUsageException(m_StringLocalizer["item:invalid", new { Item = rawInput }]);

            var amount = Context.Parameters.Length == 2 ? await Context.Parameters.GetAsync<ushort>(1) : (ushort)1;
            if (!m_Configuration.GetItemAmount(amount, out amount))
                throw new UserFriendlyException(m_StringLocalizer["items:too_much", new { UpperLimit = amount }]);

            UnturnedUser uPlayer = (UnturnedUser)Context.Actor;

            var find = m_Configuration.GetSection("restrictions:items").Get<List<Restriction>>().FirstOrDefault(x => x.ids != null && x.ids.Contains(item.ItemAsset.id));
            if (find != null)
            {
                var user = await m_UserManager.FindUserAsync(KnownActorTypes.Player, uPlayer.SteamId.ToString(), UserSearchMode.FindById);
                var check = await m_PermissionChecker.CheckPermissionAsync(user, find.bypassPermission);

                if (check == PermissionGrantResult.Default)
                {
                    await PrintAsync(m_StringLocalizer["restrictions:item_not_permitted", new { ItemName = item.ItemName }], Color.IndianRed);
                    return;
                }
            }

            await UniTask.SwitchToMainThread();
            for (ushort u = 0; u < amount; u++)
            {
                Item uItem = new(item.ItemAsset.id, EItemOrigin.ADMIN);
                uPlayer.Player.Player.inventory.forceAddItem(uItem, true);
            }

            await PrintAsync(m_StringLocalizer["item:success", new { Amount = amount, Item = item.ItemName, ID = item.ItemAssetId }]);
        }
    }
}

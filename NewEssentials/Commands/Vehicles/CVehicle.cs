using System;
using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using Microsoft.Extensions.Localization;
using NewEssentials.Helpers;
using OpenMod.API.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Microsoft.Extensions.Configuration;
using OpenMod.API.Users;
using OpenMod.API.Permissions;
using System.Collections.Generic;
using System.Linq;
using OpenMod.Core.Users;
using System.Drawing;

namespace NewEssentials.Commands.Vehicles
{
    [Command("vehicle")]
    [CommandAlias("v")]
    [CommandDescription("Spawn a vehicle")]
    [CommandSyntax("<name>/<id>")]
    [CommandActor(typeof(UnturnedUser))]
    public class CVehicle : UnturnedCommand
    {
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IConfiguration m_Configuration;
        private readonly IUserManager m_UserManager;
        private readonly IPermissionChecker m_PermissionChecker;

        public CVehicle(IStringLocalizer stringLocalizer, IServiceProvider serviceProvider, IConfiguration configuration, IUserManager userManager, IPermissionChecker permissionChecker) : base(serviceProvider)
        {
            m_StringLocalizer = stringLocalizer;
            m_Configuration = configuration;
            m_UserManager = userManager;
            m_PermissionChecker = permissionChecker;
        }

        protected override async UniTask OnExecuteAsync()
        {
            if (Context.Parameters.Length != 1)
                throw new CommandWrongUsageException(Context);

            string vehicleSearchTerm = Context.Parameters[0];
            if (!UnturnedAssetHelper.GetVehicle(vehicleSearchTerm, out VehicleAsset vehicle))
                throw new UserFriendlyException(m_StringLocalizer["vehicle:invalid",
                    new { Vehicle = vehicleSearchTerm }]);

            UnturnedUser uPlayer = (UnturnedUser)Context.Actor;

            var find = m_Configuration.GetSection("restrictions:vehicles").Get<List<Restriction>>().FirstOrDefault(x => x.ids != null && x.ids.Contains(vehicle.id));
            if (find != null)
            {
                var user = await m_UserManager.FindUserAsync(KnownActorTypes.Player, uPlayer.SteamId.ToString(), UserSearchMode.FindById);
                var check = await m_PermissionChecker.CheckPermissionAsync(user, find.bypassPermission);

                if (check == PermissionGrantResult.Default)
                {
                    await PrintAsync(m_StringLocalizer["restrictions:vehicle_not_permitted", new { VehicleName = vehicle.vehicleName }], Color.IndianRed);
                    return;
                }
            }

            await UniTask.SwitchToMainThread();
            if (VehicleTool.giveVehicle(((UnturnedUser)Context.Actor).Player.Player, vehicle.id))
                await Context.Actor.PrintMessageAsync(m_StringLocalizer["vehicle:success",
                    new { Vehicle = vehicle.vehicleName }]);
            else
                throw new UserFriendlyException(m_StringLocalizer["vehicle:failure"]);
        }
    }
}

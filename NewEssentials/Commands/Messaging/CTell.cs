﻿using System;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using OpenMod.Core.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Permissions;
using SDG.Unturned;
using UnityEngine;
using Command = OpenMod.Core.Commands.Command;

namespace NewEssentials.Commands.Messaging
{
    [UsedImplicitly]
    [Command("tell")]
    [CommandAlias("pm")]
    [CommandDescription("Send a player a private message")]
    [CommandSyntax("<player> <message>")]
    public class CTell : Command
    {
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly IStringLocalizer m_StringLocalizer;

        public CTell(IPermissionChecker permissionChecker, IStringLocalizer stringLocalizer, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_PermissionChecker = permissionChecker;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            string permission = "newess.tell";
            if (await m_PermissionChecker.CheckPermissionAsync(Context.Actor, permission) == PermissionGrantResult.Deny)
                throw new NotEnoughPermissionException(Context, permission);

            if (Context.Parameters.Length < 2)
                throw new CommandWrongUsageException(Context);

            string recipientName = Context.Parameters[0];
            if (!PlayerTool.tryGetSteamPlayer(recipientName, out SteamPlayer recipient))
                throw new UserFriendlyException(m_StringLocalizer["tpa:invalid_recipient",
                    new {Recipient = recipientName}]);
            
            StringBuilder messageBuilder = new StringBuilder();
            for(int i = 1; i < Context.Parameters.Length; i++)
                messageBuilder.Append(await Context.Parameters.GetAsync<string>(i) + " ");

            string completeMessage = messageBuilder.ToString();
            await UniTask.SwitchToMainThread();

            await Context.Actor.PrintMessageAsync(m_StringLocalizer["tell:sent",
                new {Recipient = recipient.playerID.characterName, Message = completeMessage}]);
            
            ChatManager.serverSendMessage(
                m_StringLocalizer["tell:received", new {Sender = Context.Actor.DisplayName, Message = completeMessage}],
                Color.white,
                toPlayer: recipient);
        }
    }
}
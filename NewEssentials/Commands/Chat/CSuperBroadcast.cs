using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using NewEssentials.Chat;
using OpenMod.Core.Commands;
using SDG.Unturned;
using UnityEngine;
using Color = System.Drawing.Color;
using Command = OpenMod.Core.Commands.Command;

namespace NewEssentials.Commands.Chat
{
    public class CSuperBroadcast : Command
    {

        private readonly IBroadcastingService m_Broadcasting;
        private readonly IConfiguration m_Configuration;
        private readonly IStringLocalizer m_Localizer;
        
        public CSuperBroadcast(IServiceProvider serviceProvider, IBroadcastingService broadcasting, IConfiguration config, IStringLocalizer localizer) : base(serviceProvider)
        {
            m_Broadcasting = broadcasting;
            m_Configuration = config;
            m_Localizer = localizer;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length < 1 || Context.Parameters.Length > 2)
                throw new CommandWrongUsageException("/superbroadcast \"your text here\" <time>");
            

            if (m_Broadcasting.IsActive)
            {
                await Context.Actor.PrintMessageAsync( m_Localizer["broadcasting:is_active"], Color.Red);
                return;
            }

            switch (Context.Parameters.Length)
            {
                case 1:
                    await m_Broadcasting.StartBroadcast(m_Configuration.GetValue<int>("broadcasting:defaultBroadcastDuration"), Context.Parameters[0]);
                    break;
                case 2:
                {
                    if (!float.TryParse(Context.Parameters[1], out float time))
                        throw new CommandParameterParseException("Incorrect parameter", "time", typeof(float));
                    
                    float limit = m_Configuration.GetValue<float>("broadcasting:broadcastTimeLimit");

                    if (limit > 0 && time > limit)
                    {
                        await Context.Actor.PrintMessageAsync(m_Localizer["broadcasting:too_long"], Color.Red);
                        return;
                    }

                    await Context.Actor.PrintMessageAsync(m_Localizer["broadcasting:success"], Color.Green);
                    await m_Broadcasting.StartBroadcast( (int) time, Context.Parameters[0]);
                    break;
                }
            }
        }
    }
}
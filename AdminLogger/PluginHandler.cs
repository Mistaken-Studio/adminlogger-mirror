// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Discord_Webhook;
using Exiled.API.Enums;
using Exiled.API.Features;
using Mistaken.API;

namespace Mistaken.AdminLogger
{
    /// <inheritdoc/>
    internal class PluginHandler : Plugin<Config>
    {
        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override string Name => "AdminLogger";

        /// <inheritdoc/>
        public override string Prefix => "MALOGGER";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Lowest;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(5, 0, 0);

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            Mistaken.Events.Handlers.CustomEvents.SendingCommand += this.CustomEvents_SendingCommand;
            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Mistaken.Events.Handlers.CustomEvents.SendingCommand -= this.CustomEvents_SendingCommand;
            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }

        private void CustomEvents_SendingCommand(Events.EventArgs.SendingCommandEventArgs ev)
        {
            string[] args = ev.Arguments;
            string command = ev.Command;

            // args = args.Skip(1).ToArray();
            Player senderPlayer = ev.Admin;
            Log.Debug("CustomEvents_SendingCommand", PluginHandler.Instance.Config.VerbouseOutput);
            if (string.IsNullOrWhiteSpace(command))
                return;

            if (command.StartsWith("$"))
                return;

            if (command.StartsWith("@"))
            {
                this.SendCommand("AdminChat", command.Substring(1) + string.Join(" ", args), senderPlayer, null);
                return;
            }

            Log.Debug($"Admin: {senderPlayer?.Nickname ?? "Console"} Command: {command} {string.Join(" ", args)}", PluginHandler.Instance.Config.VerbouseOutput);

            switch (command.ToLower())
            {
                case "$":
                case "request_data":
                case "ban2":
                case "ban":
                case "confirm":
                    break;
                case "server_event":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), string.Empty, senderPlayer, null);
                    else
                        this.SendCommand(command.ToLower(), args[0], senderPlayer, null);
                    break;

                case "roundrestart":
                case "reconnectrs":
                case "lockdown":
                case "forcestart":
                    this.SendCommand(command.ToLower(), "None", senderPlayer, null);
                    break;

                case "goto":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), string.Empty, senderPlayer, null);
                    else
                        this.SendCommand(command.ToLower(), "None", senderPlayer, RealPlayers.Get(args[0]));

                    break;
                case "ball":
                case "canadel":
                case "flash":
                case "grenade":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), string.Empty, senderPlayer, null);
                    else
                        this.SendCommand(command.ToLower(), args[0], senderPlayer, RealPlayers.Get(args[0].Split('.')[0]));

                    break;
                case "tpall":
                case "bring":
                case "heal":
                    {
                        if (args.Length == 0)
                            break;
                        this.SendCommand(command.ToLower(), "NONE", senderPlayer, RealPlayers.Get(args[0]?.Split('.')?[0]));
                    }

                    break;
                case "destroy":
                case "unlock":
                case "lock":
                case "close":
                case "open":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), string.Empty, senderPlayer, null);
                    else
                        this.SendCommand(command.ToLower(), args[0], senderPlayer, null);
                    break;
                case "doortp":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), string.Empty, senderPlayer, null);
                    else
                        this.SendCommand(command.ToLower(), args[0] + args[1], senderPlayer, RealPlayers.Get(args[0]?.Split('.')?[0]));

                    break;

                case "roundlock":
                    this.SendCommand(command.ToLower(), (!Round.IsLocked).ToString(), senderPlayer, null);
                    break;
                case "lobbylock":
                    this.SendCommand(command.ToLower(), (!Round.IsLobbyLocked).ToString(), senderPlayer, null);
                    break;

                case "pbc":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), string.Empty, senderPlayer, null);
                    else
                    {
                        Player player = RealPlayers.Get(args[0]?.Split('.')?[0]);
                        this.SendCommand(command.ToLower(), string.Join(" ", args.Skip(1)), senderPlayer, player);
                    }

                    break;
                case "bc":
                case "cassie_silent":
                case "cassie_sl":
                case "cassie":
                    this.SendCommand(command.ToLower(), string.Join(" ", args), senderPlayer, null);
                    break;

                case "give":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), "NONE", senderPlayer, null);
                    else
                    {
                        Player player = null;
                        if (args.Length > 0)
                            player = RealPlayers.Get(args[0].Split('.')?[0]);
                        this.SendCommand(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((ItemType)int.Parse(args[1])).ToString()), senderPlayer, player);
                    }

                    break;

                case "fc":
                case "forceclass":
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), "NONE", senderPlayer, null);
                    else
                    {
                        Player player = null;
                        if (args.Length > 0)
                            player = RealPlayers.Get(args[0].Split('.')?[0]);
                        this.SendCommand(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((RoleType)sbyte.Parse(args[1])).ToString()), senderPlayer, player);
                    }

                    break;

                default:
                    if (args.Length == 0)
                        this.SendCommand(command.ToLower(), "NONE", senderPlayer, null);
                    else
                    {
                        Player player = null;
                        if (args.Length > 0)
                            player = RealPlayers.Get(args[0].Split('.')?[0]);
                        string argString = string.Join(" ", args);
                        this.SendCommand(command.ToLower(), string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, senderPlayer, player);
                    }

                    break;
            }
        }

        private async void SendCommand(string command, string arg, Player sender, Player user)
        {
            if (sender == null)
                return;
            string adminString;
            if (sender.AuthenticationType == Exiled.API.Enums.AuthenticationType.Steam)
                adminString = $"[{sender.Nickname}](https://steamcommunity.com/profiles/{sender.UserId.Split('@')[0]})";
            else if (sender.AuthenticationType == Exiled.API.Enums.AuthenticationType.Discord)
                adminString = $"{sender.Nickname} (<@{sender.UserId.Split('@')[0]}>)";
            else
                adminString = sender.UserId;

            string userString = null;
            if (user != null)
            {
                if (user.AuthenticationType == Exiled.API.Enums.AuthenticationType.Steam)
                    userString = $"[{user.Nickname}](https://steamcommunity.com/profiles/{user.UserId.Split('@')[0]})";
                else if (user.AuthenticationType == Exiled.API.Enums.AuthenticationType.Discord)
                    userString = $"{user.Nickname} (<@{user.UserId.Split('@')[0]}>)";
                else
                    userString = user.UserId;
            }

            var response = await new Webhook(PluginHandler.Instance.Config.WebhookLink)
                .AddMessage((msg) =>
                msg
                .WithAvatar(PluginHandler.Instance.Config.WebhookAvatar)
                .WithUsername(PluginHandler.Instance.Config.WebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor(command == "AdminChat" ? "AdminChat" : $"Command: {command}", null, null, null)
                        .WithColor(255, 0, 0)
                        .WithField("User", string.IsNullOrWhiteSpace(userString ?? adminString) ? "ADMIN IS NULL" : (userString ?? adminString), true)
                        .WithField("Admin", string.IsNullOrWhiteSpace(adminString) ? "ADMIN IS NULL" : adminString, true)
                        .WithField("Server", $"{Server.IpAddress}:{Server.Port}", true)
                        .WithField("Arg", string.IsNullOrWhiteSpace(arg) ? "NO ARGS" : arg)
                        .WithCurrentTimestamp()
                    ;
                })).Send();
            Exiled.API.Features.Log.Debug(response, PluginHandler.Instance.Config.VerbouseOutput);

            // APILib.API.SendLogs(sernderPlayer.UserId, userId, command, arg, ServerConsole.Ip, Server.Port.ToString());
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="LoggerHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mistaken.API;
using Mistaken.API.Diagnostics;

namespace Mistaken.AdminLogger
{
    /// <inheritdoc/>
    public class LoggerHandler : Module
    {
        /// <summary>
        /// Logs remote command.
        /// </summary>
        /// <param name="command">Command content.</param>
        /// <param name="adminUid">AdminId.</param>
        /// <param name="userId">UserId.</param>
        public static void SendRemoteCommand(string command, string adminUid, string userId = "0")
            => APILib.API.SendLogs(adminUid, userId, "RemoteCommand", command, ServerConsole.Ip, Server.Port.ToString());

        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public LoggerHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
        }

        /// <inheritdoc/>
        public override bool IsBasic => true;

        /// <inheritdoc/>
        public override string Name => nameof(LoggerHandler);

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.SendingRemoteAdminCommand += this.Handle<Exiled.Events.EventArgs.SendingRemoteAdminCommandEventArgs>((ev) => this.Server_SendingRemoteAdminCommand(ev));
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.SendingRemoteAdminCommand -= this.Handle<Exiled.Events.EventArgs.SendingRemoteAdminCommandEventArgs>((ev) => this.Server_SendingRemoteAdminCommand(ev));
        }

        internal static void SendCommand(string command, string arg, Exiled.Events.EventArgs.SendingRemoteAdminCommandEventArgs ev, string userId)
        {
            if (ev.Sender == null)
                return;
            APILib.API.SendLogs(ev.Sender.UserId, userId, command, arg, ServerConsole.Ip, Server.Port.ToString());
        }

        private void Server_SendingRemoteAdminCommand(Exiled.Events.EventArgs.SendingRemoteAdminCommandEventArgs ev)
        {
            if (string.IsNullOrWhiteSpace(ev.Name))
                return;

            if (ev.Name.StartsWith("@"))
            {
                SendCommand("AdminChat", ev.Name.Substring(1) + string.Join(" ", ev.Arguments), ev, "0");
                return;
            }

            this.Log.Info($"Admin: {ev.Sender?.Nickname ?? "Console"} Command: {ev.Name} {string.Join(" ", ev.Arguments)}");

            switch (ev.Name.ToLower())
            {
                case "request_data":
                case "ban2":
                case "ban":
                case "confirm":
                    break;
                case "server_event":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), string.Empty, ev, "0");
                    else
                        SendCommand(ev.Name.ToLower(), ev.Arguments[0], ev, "0");
                    break;

                case "roundrestart":
                case "reconnectrs":
                case "lockdown":
                case "forcestart":
                    SendCommand(ev.Name.ToLower(), "None", ev, "0");
                    break;

                case "goto":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), string.Empty, ev, "0");
                    else
                    {
                        Player player = Player.Get(ev.Arguments[0]);
                        SendCommand(ev.Name.ToLower(), "None", ev, player?.UserId);
                    }

                    break;
                case "ball":
                case "canadel":
                case "flash":
                case "grenade":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), string.Empty, ev, "0");
                    else
                    {
                        if (ev.Arguments[0].Split('.').Length > 2)
                        {
                            Player player = RealPlayers.Get(ev.Arguments[0].Split('.')[0]);
                            SendCommand(ev.Name.ToLower(), ev.Arguments[0], ev, player?.UserId);
                        }
                        else
                        {
                            Player player = RealPlayers.Get(ev.Arguments[0]);
                            SendCommand(ev.Name.ToLower(), "NONE", ev, player?.UserId);
                        }
                    }

                    break;
                case "tpall":
                case "bring":
                case "heal":
                case "iunmute":
                case "unmute":
                case "imute":
                case "mute":
                    {
                        if (ev.Arguments.Count == 0)
                            break;
                        string playerstring = ev.Arguments[0]?.Split('.')?[0];
                        Player player = RealPlayers.Get(playerstring);

                        SendCommand(ev.Name.ToLower(), "NONE", ev, player?.UserId);
                    }

                    break;
                case "destroy":
                case "unlock":
                case "lock":
                case "close":
                case "open":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), string.Empty, ev, "0");
                    else
                        SendCommand(ev.Name.ToLower(), ev.Arguments[0], ev, "0");
                    break;
                case "doortp":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), string.Empty, ev, "0");
                    else
                    {
                        Player player = RealPlayers.Get(ev.Arguments[0]?.Split('.')?[0]);
                        SendCommand(ev.Name.ToLower(), ev.Arguments[0] + ev.Arguments[1], ev, player?.UserId);
                    }

                    break;
                case "dropall":
                case "clean":
                    if (ev.Arguments.Count < 0)
                        SendCommand(ev.Name.ToLower(), "None", ev, "0");
                    else if (ev.Arguments.Count < 1)
                        SendCommand(ev.Name.ToLower(), ev.Arguments[0], ev, "0");
                    else
                        SendCommand(ev.Name.ToLower(), ev.Arguments[0] + " " + ev.Arguments[1], ev, "0");
                    break;
                case "utag":
                case "rtag":
                case "updatetag":
                case "requesttag":
                case "refreshtag":
                    SendCommand(ev.Name.ToLower(), string.Join(" ", ev.Arguments), ev, "0");
                    break;
                case "em":
                case "eventmanager":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), "NONE", ev, "0");
                    else
                    {
                        switch (ev.Arguments[0].ToLower())
                        {
                            case "f":
                            case "force":
                                    SendCommand(ev.Name.ToLower(), "force " + (ev.Arguments.Count == 1 ? string.Empty : ev.Arguments[1]), ev, "0");
                                    break;
                            case "l":
                            case "list":
                                    SendCommand(ev.Name.ToLower(), "list", ev, "0");
                                    break;
                            default:
                                    SendCommand(ev.Name.ToLower(), string.Join(" ", ev.Arguments), ev, "0");
                                    break;
                        }
                    }

                    break;
                case "roundlock":
                    SendCommand(ev.Name.ToLower(), (!Round.IsLocked).ToString(), ev, "0");
                    break;
                case "lobbylock":
                    SendCommand(ev.Name.ToLower(), (!Round.IsLobbyLocked).ToString(), ev, "0");
                    break;
                case "warhead":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), "NONE", ev, "0");
                    else
                        SendCommand(ev.Name.ToLower(), string.Join(" ", ev.Arguments), ev, "0");
                    break;
                case "pbc":
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), string.Empty, ev, "0");
                    else
                    {
                        Player player = RealPlayers.Get(ev.Arguments[0]?.Split('.')?[0]);
                        SendCommand(ev.Name.ToLower(), string.Join(" ", ev.Arguments.Skip(1)), ev, player?.UserId ?? "0");
                    }

                    break;
                case "bc":
                case "cassie_silent":
                case "cassie_sl":
                case "cassie":
                    SendCommand(ev.Name.ToLower(), string.Join(" ", ev.Arguments), ev, "0");
                    break;
                default:
                    if (ev.Arguments.Count == 0)
                        SendCommand(ev.Name.ToLower(), "NONE", ev, "0");
                    else
                    {
                        string playerstring = string.Empty;
                        if (ev.Arguments.Count >= 1)
                            playerstring = ev.Arguments[0]?.Split('.')?[0];
                        Player player = null;
                        if (playerstring != string.Empty)
                            player = RealPlayers.Get(playerstring);
                        string argString = string.Join(" ", ev.Arguments);
                        SendCommand(ev.Name.ToLower(), string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, ev, player?.UserId ?? "0");
                    }

                    break;
            }
        }
    }
}

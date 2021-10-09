// -----------------------------------------------------------------------
// <copyright file="CommandLoggingPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Discord_Webhook;
using Exiled.API.Features;
using HarmonyLib;
using Mistaken.API;
using Mistaken.API.Extensions;
using RemoteAdmin;

namespace Mistaken.AdminLogger
{
    [HarmonyPatch(typeof(CommandProcessor), "ProcessQuery")]
    internal class CommandLoggingPatch
    {
        internal static async void SendCommand(string command, string arg, Player sender, Player user)
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
                        .WithField("User", userString ?? adminString, true)
                        .WithField("Admin", adminString, true)
                        .WithField("Server", $"{Server.IpAddress}:{Server.Port}", true)
                        .WithField("Arg", arg)
                        .WithCurrentTimestamp()
                    ;
                })).Send();
            Exiled.API.Features.Log.Debug(response, PluginHandler.Instance.Config.VerbouseOutput);

            // APILib.API.SendLogs(sernderPlayer.UserId, userId, command, arg, ServerConsole.Ip, Server.Port.ToString());
        }

        internal static void Prefix(string q, CommandSender sender)
        {
            string[] args = q.Split(' ');
            string command = args.First();
            args = args.Skip(1).ToArray();

            Player sernderPlayer = sender.GetPlayer();

            if (string.IsNullOrWhiteSpace(command))
                return;

            if (command.StartsWith("@"))
            {
                SendCommand("AdminChat", command.Substring(1) + string.Join(" ", args), sernderPlayer, null);
                return;
            }

            Log.Debug($"Admin: {sernderPlayer?.Nickname ?? "Console"} Command: {command} {string.Join(" ", args)}", PluginHandler.Instance.Config.VerbouseOutput);

            switch (command.ToLower())
            {
                case "request_data":
                case "ban2":
                case "ban":
                case "confirm":
                    break;
                case "server_event":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), string.Empty, sernderPlayer, null);
                    else
                        SendCommand(command.ToLower(), args[0], sernderPlayer, null);
                    break;

                case "roundrestart":
                case "reconnectrs":
                case "lockdown":
                case "forcestart":
                    SendCommand(command.ToLower(), "None", sernderPlayer, null);
                    break;

                case "goto":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), string.Empty, sernderPlayer, null);
                    else
                        SendCommand(command.ToLower(), "None", sernderPlayer, RealPlayers.Get(args[0]));

                    break;
                case "ball":
                case "canadel":
                case "flash":
                case "grenade":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), string.Empty, sernderPlayer, null);
                    else
                        SendCommand(command.ToLower(), args[0], sernderPlayer, RealPlayers.Get(args[0].Split('.')[0]));

                    break;
                case "tpall":
                case "bring":
                case "heal":
                case "iunmute":
                case "unmute":
                case "imute":
                case "mute":
                    {
                        if (args.Length == 0)
                            break;
                        SendCommand(command.ToLower(), "NONE", sernderPlayer, RealPlayers.Get(args[0]?.Split('.')?[0]));
                    }

                    break;
                case "destroy":
                case "unlock":
                case "lock":
                case "close":
                case "open":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), string.Empty, sernderPlayer, null);
                    else
                        SendCommand(command.ToLower(), args[0], sernderPlayer, null);
                    break;
                case "doortp":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), string.Empty, sernderPlayer, null);
                    else
                        SendCommand(command.ToLower(), args[0] + args[1], sernderPlayer, RealPlayers.Get(args[0]?.Split('.')?[0]));

                    break;

                case "roundlock":
                    SendCommand(command.ToLower(), (!Round.IsLocked).ToString(), sernderPlayer, null);
                    break;
                case "lobbylock":
                    SendCommand(command.ToLower(), (!Round.IsLobbyLocked).ToString(), sernderPlayer, null);
                    break;

                case "pbc":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), string.Empty, sernderPlayer, null);
                    else
                    {
                        Player player = RealPlayers.Get(args[0]?.Split('.')?[0]);
                        SendCommand(command.ToLower(), string.Join(" ", args.Skip(1)), sernderPlayer, player);
                    }

                    break;
                case "bc":
                case "cassie_silent":
                case "cassie_sl":
                case "cassie":
                    SendCommand(command.ToLower(), string.Join(" ", args), sernderPlayer, null);
                    break;

                case "give":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), "NONE", sernderPlayer, null);
                    else
                    {
                        Player player = null;
                        if (args.Length > 0)
                            player = RealPlayers.Get(args[0].Split('.')?[0]);
                        SendCommand(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((ItemType)int.Parse(args[1])).ToString()), sernderPlayer, player);
                    }

                    break;

                case "fc":
                case "forceclass":
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), "NONE", sernderPlayer, null);
                    else
                    {
                        Player player = null;
                        if (args.Length > 0)
                            player = RealPlayers.Get(args[0].Split('.')?[0]);
                        SendCommand(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((RoleType)sbyte.Parse(args[1])).ToString()), sernderPlayer, player);
                    }

                    break;

                default:
                    if (args.Length == 0)
                        SendCommand(command.ToLower(), "NONE", sernderPlayer, null);
                    else
                    {
                        Player player = null;
                        if (args.Length > 0)
                            player = RealPlayers.Get(args[0].Split('.')?[0]);
                        string argString = string.Join(" ", args);
                        SendCommand(command.ToLower(), string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sernderPlayer, player);
                    }

                    break;
            }
        }
    }
}

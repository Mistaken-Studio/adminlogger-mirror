// -----------------------------------------------------------------------
// <copyright file="Plugin.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Discord_Webhook;
using HarmonyLib;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace Mistaken.AdminLogger
{
    internal sealed class Plugin
    {
        public static Plugin Instance { get; private set; }

        [PluginConfig]
#pragma warning disable SA1401 // Fields should be private
        public Config Config;
#pragma warning restore SA1401 // Fields should be private

        private static readonly Harmony _harmony = new("mistaken.adminlogger.patch");

        [PluginPriority(LoadPriority.Lowest)]
        [PluginEntryPoint("Admin Logger", "1.0.0", "Admin Logger", "Mistaken Devs")]
        private void Load()
        {
            Instance = this;
            EventManager.RegisterEvents(this);
            _harmony.PatchAll();
        }

        [PluginUnload]
        private void Unload()
        {
            _harmony.UnpatchAll();
        }

        private string FormatUserId(Player player)
        {
            if (player is null)
                return "NONE";

            var split = player.UserId.Split('@');

            if (split[1] == "steam")
                return $"[{player.Nickname}](https://steamcommunity.com/profiles/{split[0]}) ({player.UserId})";
            else if (split[1] == "discord")
                return $"{player.Nickname} (<@{split[0]}>) ({player.UserId})";
            else
                return player.UserId;
        }

        private void OnPlayerAdminChat(Player player, string query)
        {
            Log.Debug(query, Config.VerboseOutput);
            ProcessCommand(player, query, Array.Empty<string>());
        }

        [PluginEvent(ServerEventType.PlayerRemoteAdminCommand)]
        private void OnPlayerRemoteAdminCommand(Player player, string command, string[] arguments)
        {
            ProcessCommand(player, command, arguments);
        }

        [PluginEvent(ServerEventType.PlayerCheaterReport)]
        private void OnPlayerCheaterReport(Player issuer, Player reported, string reason)
        {
            SendCheaterReport(issuer, reported, reason);
        }

        private void ProcessCommand(Player sender, string command, string[] args)
        {
            Log.Debug("ProcessCommand", Config.VerboseOutput);
            command = command.ToLower();

            if (string.IsNullOrWhiteSpace(command))
                return;

            if (command.StartsWith("$"))
                return;

            if (command.StartsWith("@"))
            {
                SendWebhook("AdminChat", command.Substring(1), sender, null);
                return;
            }

            Log.Debug($"Admin: {sender?.Nickname ?? "Console"} Command: {command} {string.Join(" ", args)}", Config.VerboseOutput);

            // ZROBIĆ BY WYKRYWAŁO WSZYSTKIE OSOBY A NIE PIERWSZĄ LEPSZĄ
            Player user = null;
            if (args.Length > 0 && int.TryParse(args[0].Split('.')[0], out int value))
                user = Player.Get<Player>(value);

            switch (command)
            {
                case "$":
                case "request_data":
                case "ban2":
                case "ban":
                case "confirm":
                    break;
                case "server_event":
                    if (args.Length == 0)
                        SendWebhook(command, string.Empty, sender, null);
                    else
                        SendWebhook(command, args[0], sender, null);
                    break;

                case "roundrestart":
                case "reconnectrs":
                case "lockdown":
                case "forcestart":
                    SendWebhook(command, "None", sender, null);
                    break;

                case "goto":
                    if (args.Length == 0)
                        SendWebhook(command, string.Empty, sender, null);
                    else
                        SendWebhook(command, "None", sender, user);

                    break;

                case "ball":
                case "canadel":
                case "flash":
                case "grenade":
                    if (args.Length == 0)
                        SendWebhook(command, string.Empty, sender, null);
                    else
                        SendWebhook(command, args[0], sender, user);

                    break;

                case "tpall":
                case "bring":
                case "heal":
                    {
                        if (args.Length == 0)
                            break;

                        SendWebhook(command, "NONE", sender, user);
                    }

                    break;

                case "destroy":
                case "unlock":
                case "lock":
                case "close":
                case "open":
                    if (args.Length == 0)
                        SendWebhook(command, string.Empty, sender, null);
                    else
                        SendWebhook(command, args[0], sender, null);
                    break;

                case "doortp":
                    if (args.Length == 0)
                        SendWebhook(command, string.Empty, sender, null);
                    else
                        SendWebhook(command, args[0] + args[1], sender, user);

                    break;

                case "roundlock":
                    SendWebhook(command, (!Round.IsLocked).ToString(), sender, null);
                    break;

                case "lobbylock":
                    SendWebhook(command, (!Round.IsLobbyLocked).ToString(), sender, null);
                    break;

                case "pbc":
                    if (args.Length == 0)
                        SendWebhook(command, string.Empty, sender, null);
                    else
                        SendWebhook(command, string.Join(" ", args.Skip(1)), sender, user);

                    break;

                case "bc":
                case "cassie_silent":
                case "cassie_sl":
                case "cassie":
                    SendWebhook(command, string.Join(" ", args), sender, null);
                    break;

                case "give":
                    if (args.Length == 0)
                        SendWebhook(command, "NONE", sender, null);
                    else
                        SendWebhook(command, args[0] + " " + (args.Length < 2 ? "NONE" : ((ItemType)int.Parse(args[1])).ToString()), sender, user);

                    break;

                case "fc":
                case "forceclass":
                    if (args.Length == 0)
                        SendWebhook(command, "NONE", sender, null);
                    else
                        SendWebhook(command, args[0] + " " + (args.Length < 2 ? "NONE" : ((RoleTypeId)sbyte.Parse(args[1])).ToString()), sender, user);

                    break;

                default:
                    if (args.Length == 0)
                        SendWebhook(command, "NONE", sender, null);
                    else
                    {
                        string argString = string.Join(" ", args);
                        SendWebhook(command, string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sender, user);
                    }

                    break;
            }
        }

        private async void SendWebhook(string command, string arg, Player sender, Player user)
        {
            if (sender == null)
                return;

            await new Webhook(Config.WebhookLink)
                .AddMessage((msg) => msg
                .WithAvatar(Config.WebhookAvatar)
                .WithUsername(Config.WebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor(command == "AdminChat" ? "AdminChat" : $"Command: {command}", null, null, null)
                        .WithColor(255, 0, 0)
                        .WithField("User", FormatUserId(user), true)
                        .WithField("Admin", FormatUserId(sender), true)
                        .WithField("Server", $"{Server.ServerIpAddress}:{Server.Port}", true)
                        .WithField("Arg", string.IsNullOrWhiteSpace(arg) ? "NONE" : arg)
                        .WithCurrentTimestamp();
                })).Send();
        }

        private async void SendCheaterReport(Player issuer, Player reported, string reason)
        {
            await new Webhook(Config.ReportWebhookLink)
                .AddMessage((msg) => msg
                .WithAvatar(Config.ReportWebhookAvatar)
                .WithUsername(Config.ReportWebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor("CHEATER REPORT", null, null, null)
                        .WithColor(255, 0, 0)
                        .WithField("Issuer", FormatUserId(issuer), true)
                        .WithField("Reported", FormatUserId(reported), true)
                        .WithField("Server", $"{Server.ServerIpAddress}:{Server.Port}", true)
                        .WithField("Reason", reason)
                        .WithCurrentTimestamp();
                })).Send();
        }
    }
}

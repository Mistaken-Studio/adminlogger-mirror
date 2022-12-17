// -----------------------------------------------------------------------
// <copyright file="Plugin.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Discord_Webhook;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace Mistaken.AdminLogger
{
    internal sealed class Plugin
    {
        [PluginConfig]
        public Config Config;

        [PluginPriority(LoadPriority.Lowest)]
        [PluginEntryPoint("Admin Logger", "1.0.0", "Admin Logger", "Mistaken Devs")]
        public void Initialize()
        {
            Instance = this;
            EventManager.RegisterEvents(this);
            Log.Debug(this.Config.WebhookLink);
        }

        internal static Plugin Instance { get; private set; }

        private static string FormatUserId(Player player)
        {
            if (player is null)
                return string.Empty;

            var split = player.UserId.Split('@');
            Log.Debug(split[0]);
            Log.Debug(split[1]);

            if (split[1] == "steam")
                return $"[{player.Nickname}](https://steamcommunity.com/profiles/{split[0]}) ({player.UserId})";
            else if (split[1] == "discord")
                return $"{player.Nickname} (<@{split[0]}>) ({player.UserId})";
            else
                return player.UserId;
        }

        [PluginEvent(ServerEventType.PlayerRemoteAdminCommand)]
        private void OnPlayerRemoteadminCommand(Player player, string command, string[] arguments)
        {
            Log.Info($"Player {player.Nickname} ({player.UserId}) used command {command}{(arguments.Length != 0 ? $" with arguments {string.Join(", ", arguments)}" : string.Empty)}");
            this.ProcessCommand(player, command, arguments);
        }

        [PluginEvent(ServerEventType.PlayerGameConsoleCommand)]
        private void OnPlayerGameconsoleCommand(Player player, string command, string[] arguments)
        {
            Log.Info($"Player {player.Nickname} ({player.UserId}) used command {command}{(arguments.Length != 0 ? $" with arguments {string.Join(", ", arguments)}" : string.Empty)}");
            this.ProcessCommand(player, command, arguments);
        }

        [PluginEvent(ServerEventType.ConsoleCommand)]
        private void OnConsoleCommand(string command, string[] arguments)
        {
            Log.Info($"Server used command {command}{(arguments.Length != 0 ? $" with arguments {string.Join(", ", arguments)}" : string.Empty)}");
        }

        [PluginEvent(ServerEventType.PlayerCheaterReport)]
        private void OnPlayerCheaterReport(Player issuer, Player reported, string reason)
        {
            this.SendCheaterReport(issuer, reported, reason);
        }

        private async void SendCheaterReport(Player issuer, Player reported, string reason)
        {
            string issuerString = FormatUserId(issuer);
            string reportedString = FormatUserId(reported);

            var response = await new Webhook(this.Config.ReportWebhookLink)
                .AddMessage((msg) => msg
                .WithAvatar(this.Config.ReportWebhookAvatar)
                .WithUsername(this.Config.ReportWebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor("CHEATER REPORT", null, null, null)
                        .WithColor(255, 0, 0)
                        .WithField("Issuer", issuerString, true)
                        .WithField("Reported", reportedString, true)
                        .WithField("Server", $"{Server.ServerIpAddress}:{Server.Port}", true)
                        .WithField("Reason", reason)
                        .WithCurrentTimestamp();
                })).Send();

            Log.Debug(response, this.Config.VerboseOutput);
        }

        private void ProcessCommand(Player sender, string command, string[] args)
        {
            Log.Debug("ProcessCommand", this.Config.VerboseOutput);

            if (string.IsNullOrWhiteSpace(command))
                return;

            if (command.StartsWith("$"))
                return;

            if (command.StartsWith("@"))
            {
                this.SendWebhook("AdminChat", command.Substring(1) + string.Join(" ", args), sender, null);
                return;
            }

            Log.Debug($"Admin: {sender?.Nickname ?? "Console"} Command: {command} {string.Join(" ", args)}", this.Config.VerboseOutput);

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
                        this.SendWebhook(command.ToLower(), string.Empty, sender, null);
                    else
                        this.SendWebhook(command.ToLower(), args[0], sender, null);
                    break;

                case "roundrestart":
                case "reconnectrs":
                case "lockdown":
                case "forcestart":
                    this.SendWebhook(command.ToLower(), "None", sender, null);
                    break;

                case "goto":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), string.Empty, sender, null);
                    else
                        this.SendWebhook(command.ToLower(), "None", sender, Player.Get<Player>(int.Parse(args[0].Split('.')[0])));

                    break;

                case "ball":
                case "canadel":
                case "flash":
                case "grenade":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), string.Empty, sender, null);
                    else
                        this.SendWebhook(command.ToLower(), args[0], sender, Player.Get<Player>(int.Parse(args[0].Split('.')[0])));

                    break;

                case "tpall":
                case "bring":
                case "heal":
                    {
                        if (args.Length == 0)
                            break;

                        this.SendWebhook(command.ToLower(), "NONE", sender, Player.Get<Player>(int.Parse(args[0].Split('.')[0])));
                    }

                    break;

                case "destroy":
                case "unlock":
                case "lock":
                case "close":
                case "open":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), string.Empty, sender, null);
                    else
                        this.SendWebhook(command.ToLower(), args[0], sender, null);
                    break;

                case "doortp":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), string.Empty, sender, null);
                    else
                        this.SendWebhook(command.ToLower(), args[0] + args[1], sender, Player.Get<Player>(int.Parse(args[0].Split('.')[0])));

                    break;

                case "roundlock":
                    this.SendWebhook(command.ToLower(), (!Round.IsLocked).ToString(), sender, null);
                    break;

                case "lobbylock":
                    this.SendWebhook(command.ToLower(), (!Round.IsLobbyLocked).ToString(), sender, null);
                    break;

                case "pbc":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), string.Empty, sender, null);
                    else
                    {
                        Player player = Player.Get<Player>(int.Parse(args[0].Split('.')[0]));
                        this.SendWebhook(command.ToLower(), string.Join(" ", args.Skip(1)), sender, player);
                    }

                    break;

                case "bc":
                case "cassie_silent":
                case "cassie_sl":
                case "cassie":
                    this.SendWebhook(command.ToLower(), string.Join(" ", args), sender, null);
                    break;

                case "give":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), "NONE", sender, null);
                    else
                    {
                        Player player = null;

                        if (args.Length > 0)
                            player = Player.Get<Player>(int.Parse(args[0].Split('.')[0]));

                        this.SendWebhook(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((ItemType)int.Parse(args[1])).ToString()), sender, player);
                    }

                    break;

                case "fc":
                case "forceclass":
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), "NONE", sender, null);
                    else
                    {
                        Player player = null;

                        if (args.Length > 0)
                            player = Player.Get<Player>(int.Parse(args[0].Split('.')[0]));

                        this.SendWebhook(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((RoleTypeId)sbyte.Parse(args[1])).ToString()), sender, player);
                    }

                    break;

                default:
                    if (args.Length == 0)
                        this.SendWebhook(command.ToLower(), "NONE", sender, null);
                    else
                    {
                        Player player = null;

                        if (args.Length > 0)
                            player = Player.Get<Player>(int.Parse(args[0].Split('.')[0]));

                        string argString = string.Join(" ", args);
                        this.SendWebhook(command.ToLower(), string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sender, player);
                    }

                    break;
            }
        }

        private async void SendWebhook(string command, string arg, Player sender, Player user)
        {
            if (sender == null)
                return;

            string adminString = FormatUserId(sender);
            string userString = FormatUserId(user);

            var response = await new Webhook(this.Config.WebhookLink)
                .AddMessage((msg) => msg
                .WithAvatar(this.Config.WebhookAvatar)
                .WithUsername(this.Config.WebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor(command == "AdminChat" ? "AdminChat" : $"Command: {command}", null, null, null)
                        .WithColor(255, 0, 0)
                        .WithField("User", string.IsNullOrWhiteSpace(userString ?? adminString) ? "ADMIN IS NULL" : (userString ?? adminString), true)
                        .WithField("Admin", string.IsNullOrWhiteSpace(adminString) ? "ADMIN IS NULL" : adminString, true)
                        .WithField("Server", $"{Server.ServerIpAddress}:{Server.Port}", true)
                        .WithField("Arg", string.IsNullOrWhiteSpace(arg) ? "NO ARGS" : arg)
                        .WithCurrentTimestamp();
                })).Send();

            Log.Debug(response, this.Config.VerboseOutput);
        }
    }
}

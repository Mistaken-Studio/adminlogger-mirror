// -----------------------------------------------------------------------
// <copyright file="Plugin.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Discord_Webhook;
using JetBrains.Annotations;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
namespace Mistaken.AdminLogger;

internal sealed class Plugin
{
    [UsedImplicitly]
    [PluginConfig]
    public Config Config;

    [UsedImplicitly]
    [PluginPriority(LoadPriority.Lowest)]
    [PluginEntryPoint("Admin Logger", "1.0.0", "Admin Logger", "Mistaken Devs")]
    public void Initialize()
    {
        EventManager.RegisterEvents(this);
    }

    private static string formatUserId(Player player)
    {
        if (player is null)
            return string.Empty;

        var split = player.UserId.Split('@');

        return split[1] switch
        {
            "steam" => $"[{player.Nickname}](https://steamcommunity.com/profiles/{split[0]}) ({player.UserId})",
            "discord" => $"{player.Nickname} (<@{split[0]}>) ({player.UserId})",
            _ => player.UserId
        };
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.PlayerRemoteAdminCommand)]
    private void OnPlayerRemoteAdminCommand(Player player, string command, string[] arguments)
    {
        Log.Info($"Player {player.Nickname} ({player.UserId}) used command {command}{(arguments.Length != 0 ? $" with arguments {string.Join(", ", arguments)}" : string.Empty)}");
        this.processCommand(player, command, arguments);
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.PlayerGameConsoleCommand)]
    private void OnPlayerGameConsoleCommand(Player player, string command, string[] arguments)
    {
        Log.Info($"Player {player.Nickname} ({player.UserId}) used command {command}{(arguments.Length != 0 ? $" with arguments {string.Join(", ", arguments)}" : string.Empty)}");
        this.processCommand(player, command, arguments);
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.ConsoleCommand)]
    private void OnConsoleCommand(string command, string[] arguments)
    {
        Log.Info($"Server used command {command}{(arguments.Length != 0 ? $" with arguments {string.Join(", ", arguments)}" : string.Empty)}");
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.PlayerCheaterReport)]
    private void OnPlayerCheaterReport(Player issuer, Player reported, string reason)
    {
        this.sendCheaterReport(issuer, reported, reason);
    }

    private async void sendCheaterReport(Player issuer, Player reported, string reason)
    {
        var issuerString = formatUserId(issuer);
        var reportedString = formatUserId(reported);

        var response = await new Webhook(this.Config.ReportWebhookLink)
            .AddMessage(msg => msg
                .WithAvatar(this.Config.ReportWebhookAvatar)
                .WithUsername(this.Config.ReportWebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor("CHEATER REPORT")
                        .WithColor(255, 0, 0)
                        .WithField("Issuer", issuerString, true)
                        .WithField("Reported", reportedString, true)
                        .WithField("Server", $"{Server.ServerIpAddress}:{Server.Port}", true)
                        .WithField("Reason", reason)
                        .WithCurrentTimestamp();
                })).Send();

        Log.Debug(response, this.Config.VerboseOutput);
    }

    private static Player getPlayer(string arg)
        => int.TryParse(arg.Split('.')[0], out var res) ? Player.Get<Player>(res) : Server.Instance;
        

    private void processCommand(Player sender, string command, string[] args)
    {
        Log.Debug("ProcessCommand", this.Config.VerboseOutput);

        if (string.IsNullOrWhiteSpace(command))
            return;

        if (command.StartsWith("$"))
            return;

        if (command.StartsWith("@"))
        {
            this.sendWebhook("AdminChat", command.Substring(1) + string.Join(" ", args), sender, null);
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
                this.sendWebhook(command.ToLower(), args.Length == 0 ? string.Empty : args[0], sender, null);
                break;

            case "roundrestart":
            case "reconnectrs":
            case "lockdown":
            case "forcestart":
                this.sendWebhook(command.ToLower(), "None", sender, null);
                break;

            case "goto":
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), string.Empty, sender, null);
                else
                    this.sendWebhook(command.ToLower(), "None", sender, getPlayer(args[0]));

                break;

            case "ball":
            case "canadel":
            case "flash":
            case "grenade":
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), string.Empty, sender, null);
                else
                    this.sendWebhook(command.ToLower(), args[0], sender, getPlayer(args[0]));

                break;

            case "tpall":
            case "bring":
            case "heal":
            {
                if (args.Length == 0)
                    break;

                this.sendWebhook(command.ToLower(), "NONE", sender, getPlayer(args[0]));
            }

                break;

            case "destroy":
            case "unlock":
            case "lock":
            case "close":
            case "open":
                this.sendWebhook(command.ToLower(), args.Length == 0 ? string.Empty : args[0], sender, null);
                break;

            case "doortp":
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), string.Empty, sender, null);
                else
                    this.sendWebhook(command.ToLower(), args[0] + args[1], sender, getPlayer(args[0]));

                break;

            case "roundlock":
                this.sendWebhook(command.ToLower(), (!Round.IsLocked).ToString(), sender, null);
                break;

            case "lobbylock":
                this.sendWebhook(command.ToLower(), (!Round.IsLobbyLocked).ToString(), sender, null);
                break;

            case "pbc":
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), string.Empty, sender, null);
                else
                    this.sendWebhook(command.ToLower(), string.Join(" ", args.Skip(1)), sender, getPlayer(args[0]));

                break;

            case "bc":
            case "cassie_silent":
            case "cassie_sl":
            case "cassie":
                this.sendWebhook(command.ToLower(), string.Join(" ", args), sender, null);
                break;

            case "give":
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), "NONE", sender, null);
                else
                {
                    Player player = null;

                    if (args.Length > 0)
                        player = getPlayer(args[0]);

                    this.sendWebhook(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((ItemType)int.Parse(args[1])).ToString()), sender, player);
                }

                break;

            case "fc":
            case "forceclass":
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), "NONE", sender, null);
                else
                {
                    Player player = null;

                    if (args.Length > 0)
                        player = getPlayer(args[0]);

                    this.sendWebhook(command.ToLower(), args[0] + " " + (args.Length < 2 ? "NONE" : ((RoleTypeId)sbyte.Parse(args[1])).ToString()), sender, player);
                }

                break;

            default:
                if (args.Length == 0)
                    this.sendWebhook(command.ToLower(), "NONE", sender, null);
                else
                {
                    Player player = null;

                    if (args.Length > 0)
                        player = getPlayer(args[0]);

                    var argString = string.Join(" ", args);
                    this.sendWebhook(command.ToLower(), string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sender, player);
                }

                break;
        }
    }

    private async void sendWebhook(string command, string arg, Player sender, Player user)
    {
        if (sender == null)
            return;

        var adminString = formatUserId(sender);
        var userString = formatUserId(user);

        var response = await new Webhook(this.Config.WebhookLink)
            .AddMessage(msg => msg
                .WithAvatar(this.Config.WebhookAvatar)
                .WithUsername(this.Config.WebhookUsername)
                .WithEmbed(embed =>
                {
                    embed
                        .WithAuthor(command == "AdminChat" ? "AdminChat" : $"Command: {command}")
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
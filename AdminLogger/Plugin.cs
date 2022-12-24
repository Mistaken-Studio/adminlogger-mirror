using System;
using System.Linq;
using CommandSystem;
using Discord_Webhook;
using HarmonyLib;
using JetBrains.Annotations;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using RemoteAdmin;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
namespace Mistaken.AdminLogger;

internal sealed class Plugin
{
    public static Plugin Instance { get; private set; }

    [UsedImplicitly]
    [PluginConfig]
    public Config Config;

    private static readonly Harmony Harmony = new("mistaken.adminlogger.patch");

    private static Player GetPlayer(string arg)
        => int.TryParse(arg.Split('.')[0], out var res) ? Player.Get<Player>(res) : Server.Instance;

    private static string FormatUserId(Player player)
    {
        if (player is null)
            return "NONE";

        var split = player.UserId.Split('@');

        return split[1] switch
        {
            "steam" => $"[{player.Nickname}](https://steamcommunity.com/profiles/{split[0]}) ({player.UserId})",
            "discord" => $"{player.Nickname} (<@{split[0]}>) ({player.UserId})",
            "server" => "Server",
            _ => player.UserId
        };
    }

    [UsedImplicitly]
    [PluginPriority(LoadPriority.Lowest)]
    [PluginEntryPoint("Admin Logger", "1.0.0", "Admin Logger", "Mistaken Devs")]
    private void Load()
    {
        Instance = this;
        EventManager.RegisterEvents(this);
        Harmony.PatchAll();
    }

    [UsedImplicitly]
    [PluginUnload]
    private void Unload()
    {
        Harmony.UnpatchAll();
    }

    [UsedImplicitly]
    private void OnPlayerAdminChat(Player player, string query)
    {
        Log.Debug(query, Config.VerboseOutput);
        ProcessCommand(player, query, Array.Empty<string>());
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.RemoteAdminCommand)]
    private void OnPlayerRemoteAdminCommand(ICommandSender player, string command, string[] arguments)
    {
        ProcessCommand(player, command, arguments);
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.PlayerCheaterReport)]
    private void OnPlayerCheaterReport(Player issuer, Player reported, string reason)
    {
        SendCheaterReport(issuer, reported, reason);
    }

    private void ProcessCommand(ICommandSender commandSender, string command, string[] args)
    {
        var sender = commandSender is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        ProcessCommand(sender, command, args);
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

        switch (command)
        {
            case "$":
            case "request_data":
            case "ban2":
            case "ban":
            case "confirm":
                break;
            case "server_event":
                SendWebhook(command, args.Length == 0 ? string.Empty : args[0], sender, null);
                break;

            case "roundrestart":
            case "reconnectrs":
            case "lockdown":
            case "forcestart":
                SendWebhook(command, "None", sender, null);
                break;

            case "goto":
                if (args.Length == 0)
                    break;

                SendWebhook(command, "None", sender, GetPlayer(args[0]));
                break;

            case "ball":
            case "canadel":
            case "flash":
            case "grenade":
                if (args.Length == 0)
                    SendWebhook(command, string.Empty, sender, null);
                else
                    SendWebhook(command, args[0], sender, GetPlayer(args[0]));

                break;

            case "tpall":
            case "bring":
            case "heal":
                {
                    if (args.Length == 0)
                        break;

                    SendWebhook(command, "NONE", sender, GetPlayer(args[0]));
                }

                break;

            case "destroy":
            case "unlock":
            case "lock":
            case "close":
            case "open":
                SendWebhook(command, args.Length == 0 ? string.Empty : args[0], sender, null);
                break;

            case "doortp":
                if (args.Length == 0)
                    SendWebhook(command, string.Empty, sender, null);
                else if (args.Length > 1)
                    SendWebhook(command, args[0] + args[1], sender, GetPlayer(args[0]));

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
                    SendWebhook(command, string.Join(" ", args.Skip(1)), sender, GetPlayer(args[0]));

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
                {
                    var items = args.Length > 1
                        ? string.Join(
                            ", ",
                            args[1].Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(
                                x => Enum.TryParse(x, out ItemType item)
                                    ? item.ToString()
                                    : "Error (Unknown Item)"))
                        : "NONE";

                    SendWebhook(command, args[0] + " " + items, sender, GetPlayer(args[0]));
                }

                break;

            case "fc":
            case "forceclass":
                if (args.Length == 0)
                    SendWebhook(command, "NONE", sender, null);
                else
                {
                    var role = RoleTypeId.None;
                    if (args.Length > 1)
                    {
                        if (!Enum.TryParse(args[1], out role))
                            throw new ArgumentException("Invalid roleId: " + args[1], nameof(args) + "[1]");
                    }

                    SendWebhook(command, args[0] + " " + role, sender, GetPlayer(args[0]));
                }

                break;

            default:
                if (args.Length == 0)
                    SendWebhook(command, "NONE", sender, null);
                else
                {
                    string argString = string.Join(" ", args);
                    SendWebhook(command, string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sender, GetPlayer(args[0]));
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
                    .WithAuthor(command == "AdminChat" ? "AdminChat" : $"Command: {command}")
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
                    .WithAuthor("CHEATER REPORT")
                    .WithColor(255, 0, 0)
                    .WithField("Issuer", FormatUserId(issuer), true)
                    .WithField("Reported", FormatUserId(reported), true)
                    .WithField("Server", $"{Server.ServerIpAddress}:{Server.Port}", true)
                    .WithField("Reason", reason)
                    .WithCurrentTimestamp();
            })).Send();
    }
}

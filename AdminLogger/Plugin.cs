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

    // [UsedImplicitly]
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
    private void OnPlayerCheaterReport(Player issuer, Player target, string reason)
    {
        SendCheaterReport(issuer, target, reason);
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.PlayerBanned)]
    private void OnPlayerBanned(Player target, ICommandSender issuer, string reason, long duration)
    {
        var sender = issuer is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        SendKickBanWebhook(sender, target, reason, duration);
    }

    [UsedImplicitly]
    [PluginEvent(ServerEventType.PlayerKicked)]
    private void OnPlayerKicked(Player target, ICommandSender issuer, string reason)
    {
        var sender = issuer is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        SendKickBanWebhook(sender, target, reason, 0);
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
            SendCommandWebhook("AdminChat", command.Substring(1), sender, null);
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
                SendCommandWebhook(command, args.Length == 0 ? string.Empty : args[0], sender, null);
                break;

            case "roundrestart":
            case "reconnectrs":
            case "lockdown":
            case "forcestart":
                SendCommandWebhook(command, "None", sender, null);
                break;

            case "goto":
                if (args.Length == 0)
                    break;

                SendCommandWebhook(command, "None", sender, GetPlayer(args[0]));
                break;

            case "ball":
            case "canadel":
            case "flash":
            case "grenade":
                if (args.Length == 0)
                    SendCommandWebhook(command, string.Empty, sender, null);
                else
                    SendCommandWebhook(command, args[0], sender, GetPlayer(args[0]));

                break;

            case "tpall":
            case "bring":
            case "heal":
                {
                    if (args.Length == 0)
                        break;

                    SendCommandWebhook(command, "NONE", sender, GetPlayer(args[0]));
                }

                break;

            case "destroy":
            case "unlock":
            case "lock":
            case "close":
            case "open":
                SendCommandWebhook(command, args.Length == 0 ? string.Empty : args[0], sender, null);
                break;

            case "doortp":
                if (args.Length == 0)
                    SendCommandWebhook(command, string.Empty, sender, null);
                else if (args.Length > 1)
                    SendCommandWebhook(command, args[0] + args[1], sender, GetPlayer(args[0]));

                break;

            case "roundlock":
                SendCommandWebhook(command, (!Round.IsLocked).ToString(), sender, null);
                break;

            case "lobbylock":
                SendCommandWebhook(command, (!Round.IsLobbyLocked).ToString(), sender, null);
                break;

            case "pbc":
                if (args.Length == 0)
                    SendCommandWebhook(command, string.Empty, sender, null);
                else
                    SendCommandWebhook(command, string.Join(" ", args.Skip(1)), sender, GetPlayer(args[0]));

                break;

            case "bc":
            case "cassie_silent":
            case "cassie_sl":
            case "cassie":
                SendCommandWebhook(command, string.Join(" ", args), sender, null);
                break;

            case "give":
                if (args.Length == 0)
                    SendCommandWebhook(command, "NONE", sender, null);
                else
                {
                    var items = args.Length > 1
                        ? string.Join(
                            ", ",
                            args[1].Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(
                                x => Enum.TryParse(x, true, out ItemType item)
                                    ? item.ToString()
                                    : "Error (Unknown Item)"))
                        : "NONE";

                    SendCommandWebhook(command, args[0] + " " + items, sender, GetPlayer(args[0]));
                }

                break;

            case "fc":
            case "forceclass":
                if (args.Length == 0)
                    SendCommandWebhook(command, "NONE", sender, null);
                else
                {
                    var role = RoleTypeId.None;
                    if (args.Length > 1)
                    {
                        if (!Enum.TryParse(args[1], true, out role))
                            throw new ArgumentException("Invalid roleId: " + args[1], nameof(args) + "[1]");
                    }

                    SendCommandWebhook(command, args[0] + " " + role, sender, GetPlayer(args[0]));
                }

                break;

            default:
                if (args.Length == 0)
                    SendCommandWebhook(command, "NONE", sender, null);
                else
                {
                    string argString = string.Join(" ", args);
                    SendCommandWebhook(command, string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sender, GetPlayer(args[0]));
                }

                break;
        }
    }

    private async void SendCommandWebhook(string command, string arg, Player sender, Player user)
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

    private async void SendKickBanWebhook(Player issuer, Player target, string reason, long duration)
    {
        var banDur = TimeSpan.FromSeconds(duration);
        var days = banDur.Days;
        var months = (days - (days % 30)) / 30;
        days -= months * 30;

        Embed embed = new();
        embed.WithAuthor($"User {(duration > 0 ? "Banned" : "Kicked")}!");
        embed.WithField("User", FormatUserId(target), true);
        embed.WithField("Admin", FormatUserId(issuer), true);
        embed.WithField("Reason", reason);

        if (duration > 0)
        {
            embed.WithField("Duration", $"{months:00}M {days:00}d {banDur.Hours:00}h {banDur.Minutes:00}m", true);
            embed.WithField("Until", (DateTime.Now.AddSeconds(duration)).ToString("yyyy-MM-dd HH:mm:ss"), true);
        }
        else
            embed.WithField("Duration", "KICK", true);

        embed.WithField("Server", Server.Port == 7778 ? "#2 PL RP" : "#3 Non RP", true);

        embed.WithColor(255, 0, 0);
        embed.WithFooter($"{DateTime.Now:dd:MM:yyyy} • {DateTime.Now:HH:mm:ss}");
        
        await new Webhook("https://discord.com/api/webhooks/897193757294870630/F1dDKmEhTBurdMRBWgNPNKoH70V4AKKwDowBFj8950YutR7ChMrYvj3VtPTJ-b7vXYfL")
            .AddMessage(msg => msg.Embeds.Add(embed)).Send();
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

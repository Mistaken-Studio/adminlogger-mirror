using CommandSystem;
using Discord_Webhook;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using RemoteAdmin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mistaken.AdminLogger;

internal sealed class LoggingHandler
{
    public LoggingHandler()
    {
        EventManager.RegisterEvents(this);
    }

    ~LoggingHandler()
    {
        EventManager.UnregisterEvents(this);
    }

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

    private static void ProcessCommand(ICommandSender commandSender, string command, string[] args)
    {
        var sender = commandSender is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        ProcessCommand(sender, command, args);
    }

    private static void ProcessCommand(Player sender, string command, string[] args)
    {
        Log.Debug("ProcessCommand", Plugin.Instance.Config.Debug);
        command = command.ToLower();

        if (string.IsNullOrWhiteSpace(command))
            return;

        if (command.StartsWith("$"))
            return;

        if (command.StartsWith("@"))
        {
            Task.Run(() => SendCommandWebhook("AdminChat", command.Substring(1), sender, null));
            return;
        }

        Log.Debug($"Admin: {sender?.Nickname ?? "Console"} Command: {command} {string.Join(" ", args)}", Plugin.Instance.Config.Debug);

        switch (command)
        {
            case "$":
            case "request_data":
            case "ban2":
            case "ban":
            case "confirm":
                break;
            case "server_event":
                Task.Run(() => SendCommandWebhook(command, args.Length == 0 ? string.Empty : args[0], sender, null));
                break;

            case "roundrestart":
            case "reconnectrs":
            case "lockdown":
            case "forcestart":
                Task.Run(() => SendCommandWebhook(command, "None", sender, null));
                break;

            case "goto":
                if (args.Length == 0)
                    break;

                Task.Run(() => SendCommandWebhook(command, "None", sender, GetPlayer(args[0])));
                break;

            case "ball":
            case "canadel":
            case "flash":
            case "grenade":
                if (args.Length == 0)
                    Task.Run(() => SendCommandWebhook(command, string.Empty, sender, null));
                else
                    Task.Run(() => SendCommandWebhook(command, args[0], sender, GetPlayer(args[0])));

                break;

            case "tpall":
            case "bring":
            case "heal":
                {
                    if (args.Length == 0)
                        break;

                    Task.Run(() => SendCommandWebhook(command, "NONE", sender, GetPlayer(args[0])));
                }

                break;

            case "destroy":
            case "unlock":
            case "lock":
            case "close":
            case "open":
                Task.Run(() => SendCommandWebhook(command, args.Length == 0 ? string.Empty : args[0], sender, null));
                break;

            case "doortp":
                if (args.Length == 0)
                    Task.Run(() => SendCommandWebhook(command, string.Empty, sender, null));
                else if (args.Length > 1)
                    Task.Run(() => SendCommandWebhook(command, args[0] + args[1], sender, GetPlayer(args[0])));

                break;

            case "roundlock":
                Task.Run(() => SendCommandWebhook(command, (!Round.IsLocked).ToString(), sender, null));
                break;

            case "lobbylock":
                Task.Run(() => SendCommandWebhook(command, (!Round.IsLobbyLocked).ToString(), sender, null));
                break;

            case "pbc":
                if (args.Length == 0)
                    Task.Run(() => SendCommandWebhook(command, string.Empty, sender, null));
                else
                    Task.Run(() => SendCommandWebhook(command, string.Join(" ", args.Skip(1)), sender, GetPlayer(args[0])));

                break;

            case "bc":
            case "cassie_silent":
            case "cassie_sl":
            case "cassie":
                Task.Run(() => SendCommandWebhook(command, string.Join(" ", args), sender, null));
                break;

            case "give":
                if (args.Length == 0)
                    Task.Run(() => SendCommandWebhook(command, "NONE", sender, null));
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

                    Task.Run(() => SendCommandWebhook(command, args[0] + " " + items, sender, GetPlayer(args[0])));
                }

                break;

            case "fc":
            case "forceclass":
                if (args.Length == 0)
                    Task.Run(() => SendCommandWebhook(command, "NONE", sender, null));
                else
                {
                    var role = RoleTypeId.None;
                    if (args.Length > 1)
                    {
                        if (!Enum.TryParse(args[1], true, out role))
                            throw new ArgumentException("Invalid roleId: " + args[1], nameof(args) + "[1]");
                    }

                    Task.Run(() => SendCommandWebhook(command, args[0] + " " + role, sender, GetPlayer(args[0])));
                }

                break;

            default:
                if (args.Length == 0)
                    Task.Run(() => SendCommandWebhook(command, "NONE", sender, null));
                else
                {
                    string argString = string.Join(" ", args);
                    Task.Run(() => SendCommandWebhook(command, string.IsNullOrWhiteSpace(argString) ? "NONE" : argString, sender, GetPlayer(args[0])));
                }

                break;
        }
    }

    private static async Task SendCommandWebhook(string command, string arg, Player sender, Player user)
    {
        if (sender == null)
            return;

        await new Webhook(Plugin.Instance.Config.WebhookLink)
            .AddMessage((msg) => msg
            .WithAvatar(Plugin.Instance.Config.WebhookAvatar)
            .WithUsername(Plugin.Instance.Config.WebhookUsername)
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

    private static async Task SendKickBanWebhook(Player issuer, Player target, string reason, long duration)
    {
        var banDur = TimeSpan.FromSeconds(duration);
        var days = banDur.Days;
        var months = (days - (days % 30)) / 30;
        days -= months * 30;

        await new Webhook(Plugin.Instance.Config.KickBansWebhookLink)
            .AddMessage(msg => msg
            .WithEmbed(embed =>
            {
                embed
                    .WithAuthor($"User {(duration > 0 ? "Banned" : "Kicked")}!")
                    .WithField("User", FormatUserId(target), true)
                    .WithField("Admin", FormatUserId(issuer), true)
                    .WithField("Reason", reason)
                    .WithField("Server", Server.Port == 7778 ? "#2 PL RP" : "#3 Non RP", true)
                    .WithColor(255, 0, 0)
                    .WithFooter($"{DateTime.Now:dd:MM:yyyy} • {DateTime.Now:HH:mm:ss}");

                if (duration > 0)
                {
                    embed.WithField("Duration", $"{months:00}M {days:00}d {banDur.Hours:00}h {banDur.Minutes:00}m", true)
                        .WithField("Until", (DateTime.Now.AddSeconds(duration)).ToString("yyyy-MM-dd HH:mm:ss"), true);
                }
                else
                    embed.WithField("Duration", "KICK", true);
            })).Send();
    }

    private static async Task SendCheaterReport(Player issuer, Player reported, string reason)
    {
        await new Webhook(Plugin.Instance.Config.ReportWebhookLink)
            .AddMessage((msg) => msg
            .WithAvatar(Plugin.Instance.Config.ReportWebhookAvatar)
            .WithUsername(Plugin.Instance.Config.ReportWebhookUsername)
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

    private static void OnPlayerAdminChat(string query, CommandSender issuer)
    {
        Log.Debug(query, Plugin.Instance.Config.Debug);
        var sender = issuer is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        ProcessCommand(sender, query, Array.Empty<string>());
    }

    [PluginEvent(ServerEventType.RemoteAdminCommand)]
    private void OnPlayerRemoteAdminCommand(ICommandSender player, string command, string[] arguments)
    {
        ProcessCommand(player, command, arguments);
    }

    [PluginEvent(ServerEventType.PlayerCheaterReport)]
    private void OnPlayerCheaterReport(Player issuer, Player target, string reason)
    {
        Task.Run(() => SendCheaterReport(issuer, target, reason));
    }

    [PluginEvent(ServerEventType.PlayerBanned)]
    private void OnPlayerBanned(Player target, ICommandSender issuer, string reason, long duration)
    {
        var sender = issuer is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        Task.Run(() => SendKickBanWebhook(sender, target, reason, duration));
    }

    [PluginEvent(ServerEventType.PlayerKicked)]
    private void OnPlayerKicked(Player target, ICommandSender issuer, string reason)
    {
        var sender = issuer is PlayerCommandSender playerCommandSender
            ? Player.Get(playerCommandSender!.ReferenceHub)
            : Server.Instance;

        Task.Run(() => SendKickBanWebhook(sender, target, reason, 0));
    }
}

namespace Mistaken.AdminLogger;

internal sealed class Config
{
    public bool Debug { get; set; } = false;

    public string WebhookLink { get; set; } = null;

    public string WebhookUsername { get; set; } = null;

    public string WebhookAvatar { get; set; } = null;

    public string ReportWebhookLink { get; set; } = null;

    public string ReportWebhookUsername { get; set; } = null;

    public string ReportWebhookAvatar { get; set; } = null;

    public string KickBansWebhookLink { get; set; } = null;
}
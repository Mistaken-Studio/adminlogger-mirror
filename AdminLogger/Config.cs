// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;

// ReSharper disable IdentifierTypo
namespace Mistaken.AdminLogger;

[UsedImplicitly]
internal sealed class Config
{
    public bool VerboseOutput { get; set; } = false;

    public string WebhookLink { get; set; } = null;

    public string WebhookUsername { get; set; } = null;

    public string WebhookAvatar { get; set; } = null;

    public string ReportWebhookLink { get; set; } = null;

    public string ReportWebhookUsername { get; set; } = null;

    public string ReportWebhookAvatar { get; set; } = null;
}
// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;

namespace Mistaken.AdminLogger
{
    /// <inheritdoc/>
    public class PluginHandler : Plugin<Config>
    {
        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override string Name => "AdminLogger";

        /// <inheritdoc/>
        public override string Prefix => "MALOGGER";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Lowest;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(3, 0, 0, 57);

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            var harmony = new Harmony("mistaken.logger");
            harmony.PatchAll();
            // harmony.Patch(Exiled.Events.Events.Instance.Assembly.GetType("CommandLogging").GetMethod("LogCommand", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static), new HarmonyMethod(typeof(CommandLoggingPatch).GetMethod(nameof(CommandLoggingPatch.Prefix))));

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }
    }
}

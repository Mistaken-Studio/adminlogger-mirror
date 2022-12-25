// -----------------------------------------------------------------------
// <copyright file="CommandProcessorProcessQueryPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using PluginAPI.Core;
using RemoteAdmin;

namespace Mistaken.AdminLogger;

[HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
internal static class CommandProcessorProcessQueryPatch
{
    [UsedImplicitly]
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        List<CodeInstruction> newInstructions =
            NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);

        var index = newInstructions.FindIndex(x => x.opcode == OpCodes.Starg_S) - 4; // Ldstr

        var label = generator.DefineLabel();
        var label2 = generator.DefineLabel();

        newInstructions.InsertRange(
            index,
            new[]
            {
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.PropertyGetter(typeof(Plugin), nameof(Plugin.Instance))),
                new CodeInstruction(OpCodes.Ldloc_0), // PlayerCommandSender
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(
                    OpCodes.Callvirt,
                    AccessTools.PropertyGetter(
                        typeof(PlayerCommandSender),
                        nameof(PlayerCommandSender.PlayerId))), // Jeśli będą problemy to RH może był null
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.FirstMethod(
                        typeof(Player),
                        x =>
                            !x.IsGenericMethod && x.GetParameters().Length > 0 &&
                            x.GetParameters()[0].ParameterType == typeof(int))),
                new CodeInstruction(OpCodes.Br_S, label2),
                new CodeInstruction(OpCodes.Pop).WithLabels(label),
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.PropertyGetter(typeof(Server), nameof(Server.Instance))),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label2), // string q
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Plugin), "OnPlayerAdminChat")),
            });

        foreach (var instruction in newInstructions)
            yield return instruction;

        NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}
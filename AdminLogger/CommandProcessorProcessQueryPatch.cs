// -----------------------------------------------------------------------
// <copyright file="CommandProcessorProcessQueryPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using PluginAPI.Core;
using RemoteAdmin;

namespace Mistaken.AdminLogger
{
    internal static class CommandProcessorProcessQueryPatch
    {
        [HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
        internal static class ScpSpawnerSpawnableScpsPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);

                int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Starg_S) - 4; // Ldstr

                newInstructions.InsertRange(index, new CodeInstruction[]
                {
                    new(OpCodes.Call, AccessTools.PropertyGetter(typeof(Plugin), nameof(Plugin.Instance))),
                    new(OpCodes.Ldloc_0), // PlayerCommandSender
                    new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerCommandSender), nameof(PlayerCommandSender.PlayerId))), // Jeśli będą problemy to RH może był null
                    new(OpCodes.Call, AccessTools.FirstMethod(typeof(Player), x =>
                        !x.IsGenericMethod && x.GetParameters().Length > 0 && x.GetParameters()[0].ParameterType == typeof(int))),
                    new(OpCodes.Ldarg_0), // string q
                    new(OpCodes.Call, AccessTools.Method(typeof(Plugin), "OnPlayerAdminChat")),
                });

                foreach (var instruction in newInstructions)
                    yield return instruction;

                NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
            }
        }
    }
}

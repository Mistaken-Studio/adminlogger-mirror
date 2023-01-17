using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RemoteAdmin;

namespace Mistaken.AdminLogger;

[HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
internal static class CommandProcessorPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);

        var index = newInstructions.FindIndex(x => x.opcode == OpCodes.Starg_S); // Starg.s

        newInstructions.InsertRange(index, new[]
        {
            // LoggingHandler.OnPlayerAdminChat(q, sender);
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoggingHandler), "OnPlayerAdminChat")),
        });

        foreach (var instruction in newInstructions)
            yield return instruction;

        NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}
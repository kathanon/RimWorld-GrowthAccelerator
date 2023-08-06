using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace GrowthAccelerator;
[HarmonyPatch(typeof(Building_GrowthVat))]
public static class Patches_GrowthVat {
    private static readonly ConditionalWeakTable<Building_GrowthVat, Remainder> remainders = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Building_GrowthVat.NutritionConsumedPerDay), MethodType.Getter)]
    public static void NutritionConsumedPerDay_Post(Building_GrowthVat __instance, ref float __result) {
        float stat = __instance.GetStatValue(MyDefOf.kathanon_GrowthAccelerator_GrowthVatNutrientUse);
        __result *= stat;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Building_GrowthVat.Tick))]
    public static IEnumerable<CodeInstruction> Tick_Transpiler(IEnumerable<CodeInstruction> orig) {
        var pawnSpeed = AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.GrowthVatOccupantSpeed));
        bool found = false;

        foreach (var instr in orig) {
            yield return instr;

            if (instr.LoadsField(pawnSpeed)) {
                found = true;
            } else if (found && instr.opcode == OpCodes.Mul) {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.LoadField(typeof(MyDefOf), nameof(MyDefOf.kathanon_GrowthAccelerator_GrowthVatSpeed));
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                yield return CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue));
                yield return new CodeInstruction(OpCodes.Mul);
                found = false;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Building_GrowthVat.GetInspectString))]
    public static IEnumerable<CodeInstruction> GetInspectString_Transpiler(IEnumerable<CodeInstruction> orig) {
        var ticksLeft = AccessTools.PropertyGetter(
            typeof(Building_GrowthVat),
            nameof(Building_GrowthVat.EmbryoGestationTicksRemaining));
        var adjusted = AccessTools.Method(
            typeof(Patches_GrowthVat), 
            nameof(AdjustedTicksRemaining));

        foreach (var instr in orig) {
            if (instr.Calls(ticksLeft)) {
                instr.operand = adjusted;
            }

            yield return instr;
        }
    }

    public static int AdjustedTicksRemaining(Building_GrowthVat vat) {
        float speed = vat.GetStatValue(MyDefOf.kathanon_GrowthAccelerator_GrowthVatSpeed);
        return Mathf.Max(Mathf.RoundToInt(vat.EmbryoGestationTicksRemaining / speed), 0);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Building_GrowthVat.Tick))]
    public static void Tick_Post(Building_GrowthVat __instance, ref int ___startTick) {
        if (__instance.EmbryoGestationTicksRemaining <= 0) return;

        float speed = __instance.GetStatValue(MyDefOf.kathanon_GrowthAccelerator_GrowthVatSpeed);
        var remainder = remainders.GetOrCreateValue(__instance);
        float extra = speed + remainder.value - 1f;
        int ticks = Mathf.RoundToInt(extra);
        remainder.value = extra - ticks;
        ___startTick -= ticks;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Building_GrowthVat.NutritionNeeded), MethodType.Getter)]
    public static IEnumerable<CodeInstruction> NutritionNeeded_Get_Transpiler(IEnumerable<CodeInstruction> orig) {
        float? constVal = 10;
        foreach (var instr in orig) {
            if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float val && val == constVal) {
                yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instr);
                yield return CodeInstruction.Call(typeof(Patches_GrowthVat), nameof(StorageSpace));
            } else {
                yield return instr;
            }
        }
    }

    public static float StorageSpace(Building_GrowthVat vat) 
        => vat.GetStatValue(MyDefOf.kathanon_GrowthAccelerator_GrowthVatNutrientStorage);

    private class Remainder {
        public float value;
    }
}

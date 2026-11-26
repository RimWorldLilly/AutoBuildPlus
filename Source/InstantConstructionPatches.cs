using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoBuildInstantConstruction
{
    [StaticConstructorOnStartup]
    internal static class Bootstrap
    {
        static Bootstrap()
        {
            new Harmony("lilly.autobuildplus.instantconstruction")
                .PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[AutoBuild Plus] Instant construction patches loaded.");
        }
    }

    // Make each frame's remaining construction work complete in the current
    // AutoBuild pass. The original 15-tick scheduling interval is untouched.
    [HarmonyPatch]
    internal static class InstantFrameConstructionPatch
    {
        private static bool Prepare()
        {
            Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
            Type progressType = AccessTools.TypeByName("AutoBuild.AutoBuildProgress");
            return mapType != null && progressType != null &&
                   AccessTools.Method(mapType, "Advance", new[] { typeof(Frame), typeof(float), progressType }) != null;
        }

        private static MethodBase TargetMethod()
        {
            Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
            Type progressType = AccessTools.TypeByName("AutoBuild.AutoBuildProgress");

            if (mapType == null || progressType == null)
                return null;

            return AccessTools.Method(
                mapType,
                "Advance",
                new[] { typeof(Frame), typeof(float), progressType });
        }

        private static void Prefix(Frame frame, ref float work)
        {
            if (frame == null)
                return;

            float remainingWork = Math.Max(0f, frame.WorkToBuild - frame.workDone);
            float speedFactor = 1f;

            if (frame.Stuff != null)
            {
                speedFactor = StatExtension.GetStatValueAbstract(
                    frame.Stuff,
                    StatDefOf.ConstructionSpeedFactor,
                    null);
            }

            if (speedFactor <= 0f)
                speedFactor = 1f;

            float requiredBudget = remainingWork / speedFactor;
            if (work < requiredBudget)
                work = requiredBudget;
        }
    }

    // The original method stops after eight roof cells per pass. Remove that
    // cap while preserving the original AutoBuild update interval.
    [HarmonyPatch]
    internal static class RemoveRoofConstructionCapPatch
    {
        private static bool Prepare()
        {
            Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
            return mapType != null && AccessTools.Method(mapType, "BuildReadyRoofs") != null;
        }

        private static MethodBase TargetMethod()
        {
            Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
            return mapType == null ? null : AccessTools.Method(mapType, "BuildReadyRoofs");
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int matchedConstants = 0;

            // AutoBuild's original BuildReadyRoofs contains:
            // if (++num == 8) break;
            // Replace only the eight that feeds this comparison branch.
            for (int i = 0; i < codes.Count - 1; i++)
            {
                bool isEight = codes[i].opcode == OpCodes.Ldc_I4_8 ||
                    (codes[i].opcode == OpCodes.Ldc_I4 && codes[i].operand is int value && value == 8);
                bool isEqualityBranch = codes[i + 1].opcode == OpCodes.Bne_Un ||
                    codes[i + 1].opcode == OpCodes.Bne_Un_S;

                if (isEight && isEqualityBranch)
                {
                    matchedConstants++;
                    codes[i].opcode = OpCodes.Ldc_I4;
                    codes[i].operand = int.MaxValue;
                }
            }

            if (matchedConstants != 1)
            {
                Log.Error("[AutoBuild Plus] Could not uniquely identify the roof-count comparison in " +
                    "AutoBuildMap.BuildReadyRoofs. The original roof limit was left unchanged.");
                return instructions;
            }

            return codes;
        }
    }
}

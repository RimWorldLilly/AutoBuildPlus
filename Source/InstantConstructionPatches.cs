using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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

    public sealed class AutoBuildPlusMod : Mod
    {
        internal static AutoBuildPlusSettings CurrentSettings { get; private set; }

        internal static int UpdateInterval
        {
            get
            {
                int value = CurrentSettings == null ? 1 : CurrentSettings.updateInterval;
                return Math.Max(1, Math.Min(15, value));
            }
        }

        public AutoBuildPlusMod(ModContentPack content) : base(content)
        {
            CurrentSettings = GetSettings<AutoBuildPlusSettings>();
        }

        public override string SettingsCategory()
        {
            return "AutoBuild Plus";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("건설 갱신 주기 / Update interval: " + UpdateInterval + " ticks");
            float interval = listing.Slider(UpdateInterval, 1f, 15f);
            if (CurrentSettings != null)
                CurrentSettings.updateInterval = Math.Max(1, Math.Min(15, Mathf.RoundToInt(interval)));

            listing.Gap(8f);
            listing.Label("1틱은 매 게임 틱마다 갱신하고, 15틱은 원본 AutoBuild의 기본 간격입니다.");
            listing.Label("Each update processes all eligible construction projects on the map.");

            if (listing.ButtonText("기본값 복원 / Reset to default"))
            {
                if (CurrentSettings != null)
                    CurrentSettings.updateInterval = 1;
            }

            listing.End();
        }
    }

    public sealed class AutoBuildPlusSettings : ModSettings
    {
        internal int updateInterval = 1;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref updateInterval, "updateInterval", 1);
            updateInterval = Math.Max(1, Math.Min(15, updateInterval));
        }
    }

    // Complete the remaining work of each frame as soon as AutoBuild advances it.
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

    // Replace the hard-coded 15-tick scheduler with the saved mod setting.
    // Work is scaled by the same interval so non-instant work retains its
    // original per-game-tick rate.
    [HarmonyPatch]
    internal static class AutoBuildMapTickPatch
    {
        private static int transpilerState;

        private static MethodBase TargetMethod()
        {
            Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
            return mapType == null ? null : AccessTools.Method(mapType, "MapComponentTick");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int intervalIndex = -1;
            int intervalMatches = 0;
            int workIndex = -1;
            int workMatches = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (IsIntegerConstant(codes[i], 15) &&
                    i + 1 < codes.Count &&
                    codes[i + 1].opcode == OpCodes.Rem)
                {
                    intervalIndex = i;
                    intervalMatches++;
                }

                if (codes[i].opcode == OpCodes.Ldc_R4 &&
                    codes[i].operand is float value &&
                    Math.Abs(value - 15f) < 0.001f &&
                    i + 1 < codes.Count &&
                    codes[i + 1].opcode == OpCodes.Mul)
                {
                    workIndex = i;
                    workMatches++;
                }
            }

            if (intervalMatches != 1 || workMatches != 1)
            {
                transpilerState = -1;
                Log.Error("[AutoBuild Plus] Could not uniquely identify AutoBuildMap.MapComponentTick's interval constants. " +
                    "The interval and batch-processing patches were disabled to avoid changing the wrong instructions.");
                return codes;
            }

            MethodInfo intervalGetter = AccessTools.Method(
                typeof(AutoBuildMapTickPatch),
                nameof(GetUpdateInterval));
            MethodInfo workGetter = AccessTools.Method(
                typeof(AutoBuildMapTickPatch),
                nameof(GetUpdateIntervalAsFloat));

            codes[intervalIndex].opcode = OpCodes.Call;
            codes[intervalIndex].operand = intervalGetter;
            codes[workIndex].opcode = OpCodes.Call;
            codes[workIndex].operand = workGetter;

            transpilerState = 1;
            return codes;
        }

        private static bool IsIntegerConstant(CodeInstruction instruction, int expected)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_0) return expected == 0;
            if (instruction.opcode == OpCodes.Ldc_I4_1) return expected == 1;
            if (instruction.opcode == OpCodes.Ldc_I4_2) return expected == 2;
            if (instruction.opcode == OpCodes.Ldc_I4_3) return expected == 3;
            if (instruction.opcode == OpCodes.Ldc_I4_4) return expected == 4;
            if (instruction.opcode == OpCodes.Ldc_I4_5) return expected == 5;
            if (instruction.opcode == OpCodes.Ldc_I4_6) return expected == 6;
            if (instruction.opcode == OpCodes.Ldc_I4_7) return expected == 7;
            if (instruction.opcode == OpCodes.Ldc_I4_8) return expected == 8;
            if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte shortValue)
                return shortValue == expected;
            return (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int value && value == expected);
        }

        private static int GetUpdateInterval()
        {
            return AutoBuildPlusMod.UpdateInterval;
        }

        private static float GetUpdateIntervalAsFloat()
        {
            return AutoBuildPlusMod.UpdateInterval;
        }

        // AutoBuild normally returns from AdvanceProject after the first item.
        // Temporarily move each processed item to the opposite priority set so
        // the next call can advance a different item. Restore priorities at the end.
        private static void Postfix(object __instance)
        {
            if (transpilerState != 1 || __instance == null)
                return;

            try
            {
                Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
                Type progressType = AccessTools.TypeByName("AutoBuild.AutoBuildProgress");
                Type modType = AccessTools.TypeByName("AutoBuild.AutoBuildMod");
                if (mapType == null || progressType == null || modType == null)
                    return;

                FieldInfo pausedField = AccessTools.Field(mapType, "manuallyPaused");
                FieldInfo activeProjectField = AccessTools.Field(mapType, "activeProject");
                PropertyInfo mapProperty = AccessTools.Property(mapType, "Map");
                MethodInfo hasActiveRaid = AccessTools.Method(mapType, "HasActiveRaid");
                MethodInfo advanceProject = AccessTools.Method(mapType, "AdvanceProject");
                MethodInfo collectProjects = AccessTools.Method(mapType, "CollectProjects");
                MethodInfo isUrgent = AccessTools.Method(mapType, "IsUrgent");
                MethodInfo toggleUrgent = AccessTools.Method(mapType, "ToggleUrgent");
                MethodInfo handles = AccessTools.Method(mapType, "Handles");
                PropertyInfo settingsProperty = AccessTools.Property(modType, "Settings");
                PropertyInfo progressProperty = AccessTools.Property(progressType, "Current");
                PropertyInfo workPerTickProperty = AccessTools.Property(progressType, "WorkPerTick");

                if (pausedField == null || activeProjectField == null || mapProperty == null ||
                    hasActiveRaid == null || advanceProject == null || collectProjects == null ||
                    isUrgent == null || toggleUrgent == null || handles == null ||
                    settingsProperty == null || progressProperty == null || workPerTickProperty == null)
                {
                    Log.ErrorOnce("[AutoBuild Plus] Required AutoBuild members were not found; batch construction was skipped.",
                        77120411);
                    return;
                }

                if ((bool)pausedField.GetValue(__instance))
                    return;

                object map = mapProperty.GetValue(__instance, null);
                if (map == null)
                    return;

                int interval = AutoBuildPlusMod.UpdateInterval;
                int uniqueId = GetIntMember(map, "uniqueID");
                if (Find.TickManager == null ||
                    (Find.TickManager.TicksGame + uniqueId) % interval != 0)
                    return;

                object settings = settingsProperty.GetValue(null, null);
                object progress = progressProperty.GetValue(null, null);
                if (settings == null || progress == null)
                    return;

                FieldInfo allowDuringRaidsField = AccessTools.Field(settings.GetType(), "allowDuringRaids");
                bool allowDuringRaids = allowDuringRaidsField != null &&
                    (bool)allowDuringRaidsField.GetValue(settings);
                if (!allowDuringRaids && (bool)hasActiveRaid.Invoke(__instance, null))
                    return;

                float workPerTick = (float)workPerTickProperty.GetValue(progress, null);
                float work = workPerTick * interval;
                Thing previousActiveProject = activeProjectField.GetValue(__instance) as Thing;
                bool previousWasUrgent = previousActiveProject != null &&
                    (bool)isUrgent.Invoke(__instance, new object[] { previousActiveProject });

                ProcessProjectGroup(
                    __instance, map, true, previousActiveProject, previousWasUrgent,
                    settings, progress, work, advanceProject, collectProjects,
                    isUrgent, toggleUrgent, handles);

                ProcessProjectGroup(
                    __instance, map, false, previousActiveProject, previousWasUrgent,
                    settings, progress, work, advanceProject, collectProjects,
                    isUrgent, toggleUrgent, handles);
            }
            catch (Exception exception)
            {
                Exception cause = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                Log.ErrorOnce("[AutoBuild Plus] Batch construction failed: " + cause,
                    77120412);
            }
        }

        private static void ProcessProjectGroup(
            object instance,
            object map,
            bool urgent,
            Thing previousActiveProject,
            bool previousWasUrgent,
            object settings,
            object progress,
            float work,
            MethodInfo advanceProject,
            MethodInfo collectProjects,
            MethodInfo isUrgent,
            MethodInfo toggleUrgent,
            MethodInfo handles)
        {
            List<Thing> temporarilyMoved = new List<Thing>();

            try
            {
                if (previousActiveProject != null &&
                    previousWasUrgent == urgent &&
                    IsCurrentProject(previousActiveProject, map, handles))
                {
                    ToggleProjectPriority(instance, previousActiveProject, urgent, isUrgent, toggleUrgent, temporarilyMoved);
                }

                List<Thing> projects = new List<Thing>();
                collectProjects.Invoke(instance, new object[] { projects });
                int maxPasses = Math.Min(Math.Max(projects.Count + 2, 2), 8192);
                projects.Clear();

                for (int i = 0; i < maxPasses; i++)
                {
                    bool advanced = (bool)advanceProject.Invoke(
                        instance,
                        new object[] { work, settings, progress, urgent });

                    if (!advanced)
                        break;

                    Thing activeProject = GetActiveProject(instance);
                    if (activeProject == null)
                    {
                        Log.ErrorOnce("[AutoBuild Plus] AutoBuild reported progress without identifying the active project. " +
                            "The current batch was stopped to prevent a repeated pass.",
                            77120413);
                        break;
                    }

                    if (!IsCurrentProject(activeProject, map, handles))
                        continue;

                    bool activeIsUrgent = (bool)isUrgent.Invoke(instance, new object[] { activeProject });
                    if (activeIsUrgent != urgent)
                        break;

                    ToggleProjectPriority(instance, activeProject, urgent, isUrgent, toggleUrgent, temporarilyMoved);
                }
            }
            finally
            {
                for (int i = 0; i < temporarilyMoved.Count; i++)
                {
                    Thing project = temporarilyMoved[i];
                    if (IsCurrentProject(project, map, handles))
                    {
                        bool currentlyUrgent = (bool)isUrgent.Invoke(instance, new object[] { project });
                        if (currentlyUrgent != urgent)
                            toggleUrgent.Invoke(instance, new object[] { project });
                    }
                }
            }
        }

        private static void ToggleProjectPriority(
            object instance,
            Thing project,
            bool originalUrgency,
            MethodInfo isUrgent,
            MethodInfo toggleUrgent,
            List<Thing> temporarilyMoved)
        {
            bool currentlyUrgent = (bool)isUrgent.Invoke(instance, new object[] { project });
            if (currentlyUrgent == originalUrgency)
            {
                toggleUrgent.Invoke(instance, new object[] { project });
                temporarilyMoved.Add(project);
            }
        }

        private static bool IsCurrentProject(Thing project, object map, MethodInfo handles)
        {
            return project != null &&
                project.Spawned &&
                project.Map == map &&
                (bool)handles.Invoke(null, new object[] { project });
        }

        private static Thing GetActiveProject(object instance)
        {
            Type mapType = AccessTools.TypeByName("AutoBuild.AutoBuildMap");
            FieldInfo field = mapType == null ? null : AccessTools.Field(mapType, "activeProject");
            return field == null ? null : field.GetValue(instance) as Thing;
        }

        private static int GetIntMember(object instance, string name)
        {
            FieldInfo field = AccessTools.Field(instance.GetType(), name);
            if (field != null)
                return (int)field.GetValue(instance);

            PropertyInfo property = AccessTools.Property(instance.GetType(), name);
            return property == null ? 0 : (int)property.GetValue(instance, null);
        }
    }

    // Remove the original eight-roof-cells-per-update limit.
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
            int matchIndex = -1;
            int matchedConstants = 0;

            // AutoBuild's original BuildReadyRoofs contains:
            // if (++num == 8) break;
            // Replace only the eight that feeds this comparison branch.
            for (int i = 0; i < codes.Count - 1; i++)
            {
                bool isEight = codes[i].opcode == OpCodes.Ldc_I4_8 ||
                    (codes[i].opcode == OpCodes.Ldc_I4 && codes[i].operand is int value && value == 8);
                bool isEqualityBranch = codes[i + 1].opcode == OpCodes.Beq ||
                    codes[i + 1].opcode == OpCodes.Beq_S ||
                    codes[i + 1].opcode == OpCodes.Bne_Un ||
                    codes[i + 1].opcode == OpCodes.Bne_Un_S;

                if (isEight && isEqualityBranch)
                {
                    matchedConstants++;
                    matchIndex = i;
                }
            }

            if (matchedConstants != 1)
            {
                Log.Error("[AutoBuild Plus] Could not uniquely identify the roof-count comparison in " +
                    "AutoBuildMap.BuildReadyRoofs. The original roof limit was left unchanged.");
                return codes;
            }

            codes[matchIndex].opcode = OpCodes.Ldc_I4;
            codes[matchIndex].operand = int.MaxValue;
            return codes;
        }
    }
}

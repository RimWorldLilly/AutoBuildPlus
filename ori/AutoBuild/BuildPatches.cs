using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoBuild
{
	// Token: 0x02000008 RID: 8
	[StaticConstructorOnStartup]
	internal static class BuildPatches
	{
		// Token: 0x06000035 RID: 53 RVA: 0x00003580 File Offset: 0x00001780
		static BuildPatches()
		{
			new Harmony("local.autobuild.rimworld").PatchAll();
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003594 File Offset: 0x00001794
		internal static void Complete(Frame frame, Pawn worker)
		{
			Pawn pawn = BuildPatches.completingWorker;
			BuildPatches.completingWorker = worker;
			try
			{
				frame.CompleteConstruction(worker);
			}
			finally
			{
				BuildPatches.completingWorker = pawn;
			}
		}

		// Token: 0x04000022 RID: 34
		[ThreadStatic]
		private static Pawn completingWorker;

		// Token: 0x02000012 RID: 18
		[HarmonyPatch(typeof(GenConstruct), "CanConstruct", new Type[]
		{
			typeof(Thing),
			typeof(Pawn),
			typeof(bool),
			typeof(bool),
			typeof(JobDef)
		})]
		private static class PawnConstruction
		{
			// Token: 0x06000060 RID: 96 RVA: 0x00005208 File Offset: 0x00003408
			private static bool Prefix(Thing t)
			{
				return !AutoBuildMap.Handles(t);
			}
		}

		// Token: 0x02000013 RID: 19
		[HarmonyPatch(typeof(Blueprint), "TryReplaceWithSolidThing")]
		private static class BlueprintPriority
		{
			// Token: 0x06000061 RID: 97 RVA: 0x00005214 File Offset: 0x00003414
			private static void Postfix(Blueprint __instance, ref Thing createdThing, bool __result)
			{
				if (__result)
				{
					Frame frame = createdThing as Frame;
					if (frame != null)
					{
						Map map = frame.Map;
						if (map == null)
						{
							return;
						}
						AutoBuildMap component = map.GetComponent<AutoBuildMap>();
						if (component == null)
						{
							return;
						}
						component.TransferPriority(__instance, frame);
					}
				}
			}
		}

		// Token: 0x02000014 RID: 20
		[HarmonyPatch(typeof(WorkGiver_Deconstruct), "HasJobOnThing")]
		private static class PawnDeconstruction
		{
			// Token: 0x06000062 RID: 98 RVA: 0x0000524C File Offset: 0x0000344C
			private static bool Prefix(Pawn pawn, Thing t, bool forced)
			{
				if (!forced)
				{
					Faction faction = pawn.Faction;
					if (faction != null && faction.IsPlayer)
					{
						if (t == null)
						{
							return true;
						}
						Map map = t.Map;
						bool? flag;
						if (map == null)
						{
							flag = null;
						}
						else
						{
							AutoBuildMap component = map.GetComponent<AutoBuildMap>();
							flag = ((component != null) ? new bool?(component.HandlesDeconstruction(t)) : null);
						}
						bool? flag2 = flag;
						return !flag2.GetValueOrDefault();
					}
				}
				return true;
			}
		}

		// Token: 0x02000015 RID: 21
		[HarmonyPatch]
		private static class PawnSurfaceRemoval
		{
			// Token: 0x06000063 RID: 99 RVA: 0x000052B9 File Offset: 0x000034B9
			private static IEnumerable<MethodBase> TargetMethods()
			{
				return new BuildPatches.PawnSurfaceRemoval.<TargetMethods>d__0(-2);
			}

			// Token: 0x06000064 RID: 100 RVA: 0x000052C4 File Offset: 0x000034C4
			private static bool Prefix(WorkGiver_Scanner __instance, Pawn pawn, IntVec3 c, bool forced)
			{
				if (!forced)
				{
					Faction faction = pawn.Faction;
					if (faction != null && faction.IsPlayer)
					{
						RemovalOrder removalOrder;
						if (!(__instance is WorkGiver_RemoveRoof))
						{
							if (!(__instance is WorkGiver_ConstructRemoveFloor))
							{
								removalOrder = RemovalOrder.Foundation;
							}
							else
							{
								removalOrder = RemovalOrder.Floor;
							}
						}
						else
						{
							removalOrder = RemovalOrder.Roof;
						}
						RemovalOrder removalOrder2 = removalOrder;
						Map map = pawn.Map;
						if (map == null)
						{
							return true;
						}
						AutoBuildMap component = map.GetComponent<AutoBuildMap>();
						return !((component != null) ? new bool?(component.HandlesSurfaceRemoval(c, removalOrder2)) : null).GetValueOrDefault();
					}
				}
				return true;
			}
		}

		// Token: 0x02000016 RID: 22
		[HarmonyPatch]
		private static class PawnRoofConstruction
		{
			// Token: 0x06000065 RID: 101 RVA: 0x00005342 File Offset: 0x00003542
			private static IEnumerable<MethodBase> TargetMethods()
			{
				return new BuildPatches.PawnRoofConstruction.<TargetMethods>d__0(-2);
			}

			// Token: 0x06000066 RID: 102 RVA: 0x0000534B File Offset: 0x0000354B
			private static bool Prefix(Pawn pawn, IntVec3 c)
			{
				Faction faction = pawn.Faction;
				return faction == null || !faction.IsPlayer || RoofUtility.FirstBlockingThing(c, pawn.Map) != null;
			}
		}

		// Token: 0x02000017 RID: 23
		[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
		private static class MapPauseButton
		{
			// Token: 0x06000067 RID: 103 RVA: 0x00005378 File Offset: 0x00003578
			private static void Postfix(WidgetRow row, bool worldView)
			{
				if (!worldView)
				{
					Map currentMap = Find.CurrentMap;
					AutoBuildMap autoBuildMap = ((currentMap != null) ? currentMap.GetComponent<AutoBuildMap>() : null);
					if (autoBuildMap != null)
					{
						bool manuallyPaused = autoBuildMap.manuallyPaused;
						string text = Translator.Translate(manuallyPaused ? "AutoBuild_ResumeMap" : "AutoBuild_PauseMap");
						row.Gap(4f);
						Rect rect = row.ButtonIconRect(-1f);
						Event current = Event.current;
						if (current.type == null && current.button == 1 && Mouse.IsOver(rect))
						{
							current.Use();
							if (!Find.WindowStack.IsOpen<ConstructionQueueWindow>())
							{
								Find.WindowStack.Add(new ConstructionQueueWindow(autoBuildMap));
							}
						}
						if (!row.ButtonIcon((manuallyPaused ? BuildPatches.MapPauseButton.ResumeIcon : BuildPatches.MapPauseButton.PauseIcon).Texture, text + "\n" + Translator.Translate("AutoBuild_QueueClick") + "\n\n" + AutoBuildProgress.Current.Describe(), null, null, null, true, -1f))
						{
							return;
						}
						autoBuildMap.manuallyPaused = !manuallyPaused;
						SoundStarter.PlayOneShotOnCamera(manuallyPaused ? SoundDefOf.Tick_High : SoundDefOf.Tick_Low, null);
						return;
					}
				}
			}

			// Token: 0x04000053 RID: 83
			private static readonly CachedTexture PauseIcon = new CachedTexture("UI/AutoBuild/Pause");

			// Token: 0x04000054 RID: 84
			private static readonly CachedTexture ResumeIcon = new CachedTexture("UI/AutoBuild/Resume");
		}

		// Token: 0x02000018 RID: 24
		[HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new Type[]
		{
			typeof(Pawn),
			typeof(SkillDef),
			typeof(bool)
		})]
		private static class ConstructionQuality
		{
			// Token: 0x06000069 RID: 105 RVA: 0x000054D3 File Offset: 0x000036D3
			private static bool Prefix(Pawn pawn, SkillDef relevantSkill, ref QualityCategory __result)
			{
				if (BuildPatches.completingWorker == null || pawn != BuildPatches.completingWorker || relevantSkill != SkillDefOf.Construction)
				{
					return true;
				}
				__result = AutoBuildProgress.Current.RollQuality();
				return false;
			}
		}

		// Token: 0x02000019 RID: 25
		[HarmonyPatch(typeof(Frame), "GetIdeoForStyle")]
		private static class ConstructionStyle
		{
			// Token: 0x0600006A RID: 106 RVA: 0x000054FB File Offset: 0x000036FB
			private static void Postfix(Pawn worker, ref Ideo __result)
			{
				if (BuildPatches.completingWorker != null && worker == BuildPatches.completingWorker && ModsConfig.IdeologyActive)
				{
					__result = worker.Faction.ideos.PrimaryIdeo;
				}
			}
		}

		// Token: 0x0200001A RID: 26
		[HarmonyPatch(typeof(TaleRecorder), "RecordTale")]
		private static class LongConstructionTale
		{
			// Token: 0x0600006B RID: 107 RVA: 0x00005525 File Offset: 0x00003725
			private static bool Prefix(TaleDef def, object[] args)
			{
				return BuildPatches.completingWorker == null || def != TaleDefOf.CompletedLongConstructionProject || args.Length == 0 || args[0] != BuildPatches.completingWorker;
			}
		}
	}
}

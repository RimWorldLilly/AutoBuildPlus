using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AutoBuild
{
	// Token: 0x02000004 RID: 4
	public sealed class AutoBuildMap : MapComponent
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002068 File Offset: 0x00000268
		public AutoBuildMap(Map map)
			: base(map)
		{
			this.materials = new StockMaterials(map);
			this.clearance = new SiteClearance(map);
			this.deconstruction = new Deconstruction(map);
			this.surfaceRemoval = new SurfaceRemoval(map);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020D0 File Offset: 0x000002D0
		public override void MapComponentTick()
		{
			if (this.manuallyPaused || (Find.TickManager.TicksGame + this.map.uniqueID) % 15 != 0)
			{
				return;
			}
			AutoBuildSettings settings = AutoBuildMod.Settings;
			this.activeProject = null;
			if (!settings.allowDuringRaids && this.HasActiveRaid())
			{
				return;
			}
			AutoBuildProgress autoBuildProgress = AutoBuildProgress.Current;
			float num = autoBuildProgress.WorkPerTick * 15f;
			this.RemoveExpiredPriorities();
			bool flag = (this.urgentProjects.Count > 0 && this.AdvanceProject(num, settings, autoBuildProgress, true)) || (settings.autoDeconstruct && (this.surfaceRemoval.Advance(num) || this.AdvanceDeconstruction(num))) || this.AdvanceProject(num, settings, autoBuildProgress, false);
			if (this.BuildReadyRoofs(autoBuildProgress) || flag || !settings.clearObstacles)
			{
				return;
			}
			foreach (IntVec3 intVec in this.map.areaManager.BuildRoof.ActiveCells)
			{
				if (this.CanBuildRoof(intVec))
				{
					Thing thing = RoofUtility.FirstBlockingThing(intVec, this.map);
					if (thing != null && this.clearance.Advance(thing, CellRect.SingleCell(intVec), num, this.Worker))
					{
						break;
					}
				}
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002220 File Offset: 0x00000420
		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref this.manuallyPaused, "manuallyPaused", false, false);
			Scribe_Values.Look<IntVec3>(ref this.legacyRoofTarget, "roofTarget", IntVec3.Invalid, false);
			Scribe_Values.Look<float>(ref this.legacyRoofWorkDone, "roofWorkDone", 0f, false);
			if (Scribe.mode == 1)
			{
				this.RemoveExpiredPriorities();
			}
			Scribe_Collections.Look<Thing>(ref this.urgentProjects, "urgentProjects", 3);
			if (Scribe.mode == 4 && this.urgentProjects == null)
			{
				this.urgentProjects = new HashSet<Thing>();
			}
			this.deconstruction.ExposeData();
			this.surfaceRemoval.ExposeData();
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000022BC File Offset: 0x000004BC
		internal bool HandlesDeconstruction(Thing target)
		{
			AutoBuildSettings settings = AutoBuildMod.Settings;
			Building building;
			return settings.autoDeconstruct && !this.manuallyPaused && this.deconstruction.CanAdvance(target, out building) && this.map.designationManager.DesignationOn(target, DesignationDefOf.Deconstruct) != null && (settings.allowDuringRaids || !this.HasActiveRaid());
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000231C File Offset: 0x0000051C
		internal bool HandlesSurfaceRemoval(IntVec3 cell, RemovalOrder order)
		{
			AutoBuildSettings settings = AutoBuildMod.Settings;
			return settings.autoDeconstruct && !this.manuallyPaused && this.surfaceRemoval.CanAdvance(cell, order) && (settings.allowDuringRaids || !this.HasActiveRaid());
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002364 File Offset: 0x00000564
		private bool AdvanceDeconstruction(float work)
		{
			Thing thing;
			if (!this.deconstruction.Advance(work, out thing))
			{
				return false;
			}
			if (thing != null)
			{
				Faction faction = thing.Faction;
				if (faction != null)
				{
					faction.Notify_BuildingRemoved((Building)MinifyUtility.GetInnerIfMinified(thing), this.Worker);
				}
				thing.Destroy(4);
			}
			return true;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000023B0 File Offset: 0x000005B0
		private bool HasActiveRaid()
		{
			Faction ofPlayer = Faction.OfPlayer;
			foreach (Pawn pawn in this.map.mapPawns.AllPawnsSpawned)
			{
				Faction faction = pawn.Faction;
				if (faction != null && FactionUtility.HostileTo(faction, ofPlayer) && !pawn.Downed && !pawn.IsPrisoner)
				{
					Lord lord = LordUtility.GetLord(pawn);
					if (!(((lord != null) ? lord.CurLordToil : null) is LordToil_ExitMap) && (((lord != null) ? lord.LordJob : null) is LordJob_AssaultColony || ((lord != null) ? lord.LordJob : null) is LordJob_StageThenAttack || ((lord != null) ? lord.LordJob : null) is LordJob_Siege || GenHostility.IsActiveThreatToPlayer(pawn, true)))
					{
						return true;
					}
				}
			}
			foreach (Thing thing in this.map.listerThings.ThingsInGroup(43))
			{
				IActiveTransporter activeTransporter = thing as IActiveTransporter;
				if (activeTransporter != null && activeTransporter.Contents != null)
				{
					foreach (Thing thing2 in activeTransporter.Contents.innerContainer)
					{
						Pawn pawn2 = thing2 as Pawn;
						if (pawn2 != null && !pawn2.Downed)
						{
							Faction faction2 = pawn2.Faction;
							if (faction2 != null && FactionUtility.HostileTo(faction2, ofPlayer))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000A RID: 10 RVA: 0x00002568 File Offset: 0x00000768
		internal Map Map
		{
			get
			{
				return this.map;
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002570 File Offset: 0x00000770
		internal bool IsUrgent(Thing project)
		{
			return this.urgentProjects.Contains(project);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000257E File Offset: 0x0000077E
		internal void ToggleUrgent(Thing project)
		{
			if (project.Map == this.map && AutoBuildMap.Handles(project) && !this.urgentProjects.Remove(project))
			{
				this.urgentProjects.Add(project);
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000025B1 File Offset: 0x000007B1
		internal void TransferPriority(Blueprint blueprint, Frame frame)
		{
			if (this.urgentProjects.Remove(blueprint))
			{
				this.urgentProjects.Add(frame);
			}
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000025CE File Offset: 0x000007CE
		private void RemoveExpiredPriorities()
		{
			if (this.urgentProjects.Count > 0)
			{
				this.urgentProjects.RemoveWhere((Thing project) => ((project != null) ? project.Map : null) != this.map);
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000025F8 File Offset: 0x000007F8
		internal void CollectProjects(List<Thing> result)
		{
			result.Clear();
			foreach (Thing thing in this.map.listerThings.ThingsInGroup(11))
			{
				if (AutoBuildMap.Handles(thing))
				{
					result.Add(thing);
				}
			}
			foreach (Thing thing2 in this.map.listerThings.ThingsInGroup(9))
			{
				if (AutoBuildMap.Handles(thing2))
				{
					result.Add(thing2);
				}
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000026BC File Offset: 0x000008BC
		internal string QueueNotice()
		{
			if (this.manuallyPaused)
			{
				return Translator.Translate("AutoBuild_QueueManualPause");
			}
			if (!AutoBuildMod.Settings.allowDuringRaids && this.HasActiveRaid())
			{
				return Translator.Translate("AutoBuild_QueueRaidPause");
			}
			return Translator.Translate("AutoBuild_QueueHint");
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002714 File Offset: 0x00000914
		internal string ProjectStatus(Thing project, StockMaterials queueMaterials)
		{
			if (!this.CanWorkHere(project))
			{
				return Translator.Translate("AutoBuild_QueueUnavailable");
			}
			AutoBuildSettings settings = AutoBuildMod.Settings;
			Thing thing = GenConstruct.FirstBlockingThing(project, null);
			if (thing != null && (!settings.clearObstacles || !this.clearance.CanAdvance(thing)))
			{
				return TranslatorFormattedStringExtensions.Translate("AutoBuild_QueueBlocked", thing.LabelCap);
			}
			Frame frame = project as Frame;
			MaterialShortage materialShortage;
			if ((frame == null || !frame.IsCompleted()) && !queueMaterials.Plan((IConstructible)project, settings, out materialShortage))
			{
				return (materialShortage.Protected > 0) ? TranslatorFormattedStringExtensions.Translate("AutoBuild_QueueProtected", materialShortage.Material.LabelCap, materialShortage.Missing, materialShortage.Protected) : TranslatorFormattedStringExtensions.Translate("AutoBuild_QueueMissing", materialShortage.Material.LabelCap, materialShortage.Missing);
			}
			if (project == this.activeProject && !this.manuallyPaused)
			{
				return Translator.Translate("AutoBuild_QueueWorking");
			}
			return (thing != null) ? TranslatorFormattedStringExtensions.Translate("AutoBuild_QueueClearance", thing.LabelCap) : Translator.Translate("AutoBuild_QueueReady");
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002850 File Offset: 0x00000A50
		private bool AdvanceProject(float work, AutoBuildSettings settings, AutoBuildProgress progress, bool urgent)
		{
			this.CollectProjects(this.projects);
			bool flag2;
			try
			{
				foreach (Thing thing in this.projects)
				{
					if (thing.Spawned && this.IsUrgent(thing) == urgent && this.CanWorkHere(thing))
					{
						Thing thing2 = GenConstruct.FirstBlockingThing(thing, null);
						if (thing2 == null || settings.clearObstacles)
						{
							Frame frame = thing as Frame;
							bool flag = frame != null && frame.IsCompleted();
							MaterialShortage materialShortage;
							if (flag || this.materials.Plan((IConstructible)thing, settings, out materialShortage))
							{
								if (thing2 != null)
								{
									if (this.clearance.Advance(thing2, GenAdj.OccupiedRect(thing), work, this.Worker))
									{
										this.activeProject = thing;
										return true;
									}
								}
								else
								{
									if (flag)
									{
										this.activeProject = frame;
										this.Advance(frame, work, progress);
										return true;
									}
									if (frame == null)
									{
										if (!this.OpenFrame((Blueprint_Build)thing, out frame))
										{
											continue;
										}
										this.materials.Clear();
										if (!this.materials.Plan(frame, settings, out materialShortage))
										{
											continue;
										}
									}
									this.materials.DeliverTo(frame, settings.allowMapMaterials);
									if (frame.IsCompleted())
									{
										this.activeProject = frame;
										return true;
									}
								}
							}
						}
					}
				}
				flag2 = false;
			}
			finally
			{
				this.projects.Clear();
				this.materials.Clear();
			}
			return flag2;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002A00 File Offset: 0x00000C00
		internal static bool Handles(Thing project)
		{
			if (!(project is Frame) && !(project is Blueprint_Build))
			{
				return false;
			}
			Faction faction = project.Faction;
			if (faction == null || !faction.IsPlayer)
			{
				return false;
			}
			if (!(project.def.entityDefToBuild is TerrainDef))
			{
				ThingDef thingDef = project.def.entityDefToBuild as ThingDef;
				return thingDef != null && thingDef.category == 3;
			}
			return true;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002A68 File Offset: 0x00000C68
		private bool CanWorkHere(Thing project)
		{
			if (GridsUtility.Fogged(project.Position, this.map) || ForbidUtility.IsForbidden(project, Faction.OfPlayer) || FireUtility.IsBurning(project) || this.map.reservationManager.IsReserved(project))
			{
				return false;
			}
			BuildableDef entityDefToBuild = project.def.entityDefToBuild;
			Blueprint_Build blueprint_Build = project as Blueprint_Build;
			ThingDef thingDef = ((blueprint_Build != null) ? blueprint_Build.stuffToUse : project.Stuff);
			if (!GenConstruct.CanBuildOnTerrain(entityDefToBuild, project.Position, this.map, project.Rotation, project, thingDef))
			{
				return false;
			}
			ThingDef thingDef2 = entityDefToBuild as ThingDef;
			if (thingDef2 != null)
			{
				BuildingProperties building = thingDef2.building;
				if (building != null && building.isAttachment)
				{
					Thing wallAttachedTo = GenConstruct.GetWallAttachedTo(project);
					if (wallAttachedTo is Building)
					{
						thingDef2 = wallAttachedTo.def;
						if (thingDef2 != null && !thingDef2.IsFrame)
						{
							goto IL_00CA;
						}
					}
					return false;
				}
			}
			IL_00CA:
			if (entityDefToBuild.passability == 2)
			{
				using (CellRect.Enumerator enumerator = GenAdj.OccupiedRect(project).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (GridsUtility.GetFirstPawn(enumerator.Current, this.map) != null)
						{
							return false;
						}
					}
				}
				return true;
			}
			return true;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002BA4 File Offset: 0x00000DA4
		private bool OpenFrame(Blueprint_Build blueprint, out Frame frame)
		{
			Thing thing;
			bool flag2;
			bool flag = blueprint.TryReplaceWithSolidThing(this.Worker, ref thing, ref flag2);
			frame = thing as Frame;
			return flag && frame != null && frame.Spawned;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002BD8 File Offset: 0x00000DD8
		private void Advance(Frame frame, float work, AutoBuildProgress progress)
		{
			if (frame.Stuff != null)
			{
				work *= StatExtension.GetStatValueAbstract(frame.Stuff, StatDefOf.ConstructionSpeedFactor, null);
			}
			if (frame.def.entityDefToBuild is TerrainDef)
			{
				this.map.snowGrid.SetDepth(frame.Position, 0f);
				SandGrid sandGrid = this.map.sandGrid;
				if (sandGrid != null)
				{
					sandGrid.SetDepth(frame.Position, 0f);
				}
			}
			float workToBuild = frame.WorkToBuild;
			float num = Math.Min(work, Math.Max(0f, workToBuild - frame.workDone));
			frame.workDone += num;
			progress.AddWork(num);
			if (frame.workDone >= workToBuild)
			{
				BuildPatches.Complete(frame, this.Worker);
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002C9C File Offset: 0x00000E9C
		private bool CanBuildRoof(IntVec3 cell)
		{
			return !GridsUtility.Roofed(cell, this.map) && !this.map.areaManager.NoRoof[cell] && !GridsUtility.Fogged(cell, this.map) && !this.map.reservationManager.IsReserved(cell) && RoofCollapseUtility.WithinRangeOfRoofHolder(cell, this.map, false) && RoofCollapseUtility.ConnectedToRoofHolder(cell, this.map, true);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002D14 File Offset: 0x00000F14
		private bool BuildReadyRoofs(AutoBuildProgress progress)
		{
			int num = 0;
			foreach (IntVec3 intVec in this.map.areaManager.BuildRoof.ActiveCells)
			{
				if (this.CanBuildRoof(intVec) && RoofUtility.FirstBlockingThing(intVec, this.map) == null)
				{
					this.BuildRoof(intVec, progress);
					if (++num == 8)
					{
						break;
					}
				}
			}
			return num > 0;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002D98 File Offset: 0x00000F98
		private void BuildRoof(IntVec3 cell, AutoBuildProgress progress)
		{
			float num = 65f;
			if (cell == this.legacyRoofTarget)
			{
				num -= this.legacyRoofWorkDone;
				this.legacyRoofTarget = IntVec3.Invalid;
				this.legacyRoofWorkDone = 0f;
			}
			this.map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
			progress.AddWork(num);
			MoteMaker.PlaceTempRoof(cell, this.map);
			List<Thing> thingList = GridsUtility.GetThingList(cell, this.map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				BuildingProperties building = thing.def.building;
				if (building != null && building.IsMortar)
				{
					CompWakeUpDormant compWakeUpDormant = ThingCompUtility.TryGetComp<CompWakeUpDormant>(thing);
					if (compWakeUpDormant != null)
					{
						compWakeUpDormant.Activate(this.Worker, true, false, false);
					}
				}
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600001A RID: 26 RVA: 0x00002E5C File Offset: 0x0000105C
		private Pawn Worker
		{
			get
			{
				if (this.worker == null)
				{
					this.worker = new Pawn
					{
						def = ThingDefOf.Human,
						kindDef = PawnKindDefOf.Colonist,
						Name = new NameSingle(Translator.Translate("AutoBuild_WorkerName"), false)
					};
					this.worker.SetFactionDirect(Faction.OfPlayer);
					PawnComponentsUtility.CreateInitialComponents(this.worker);
				}
				return this.worker;
			}
		}

		// Token: 0x04000002 RID: 2
		private const int UpdateInterval = 15;

		// Token: 0x04000003 RID: 3
		private const int RoofCellsPerUpdate = 8;

		// Token: 0x04000004 RID: 4
		private const float RoofExperiencePerCell = 65f;

		// Token: 0x04000005 RID: 5
		private readonly List<Thing> projects = new List<Thing>();

		// Token: 0x04000006 RID: 6
		private readonly StockMaterials materials;

		// Token: 0x04000007 RID: 7
		private readonly SiteClearance clearance;

		// Token: 0x04000008 RID: 8
		private readonly Deconstruction deconstruction;

		// Token: 0x04000009 RID: 9
		private readonly SurfaceRemoval surfaceRemoval;

		// Token: 0x0400000A RID: 10
		private HashSet<Thing> urgentProjects = new HashSet<Thing>();

		// Token: 0x0400000B RID: 11
		private Thing activeProject;

		// Token: 0x0400000C RID: 12
		internal bool manuallyPaused;

		// Token: 0x0400000D RID: 13
		private IntVec3 legacyRoofTarget = IntVec3.Invalid;

		// Token: 0x0400000E RID: 14
		private float legacyRoofWorkDone;

		// Token: 0x0400000F RID: 15
		private Pawn worker;
	}
}

using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoBuild
{
	// Token: 0x0200000A RID: 10
	internal sealed class Deconstruction
	{
		// Token: 0x0600003C RID: 60 RVA: 0x00003B27 File Offset: 0x00001D27
		internal Deconstruction(Map map)
		{
			this.map = map;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003B36 File Offset: 0x00001D36
		internal void ExposeData()
		{
			Scribe_References.Look<Thing>(ref this.target, "deconstructionTarget", false);
			Scribe_Values.Look<float>(ref this.workDone, "deconstructionWorkDone", 0f, false);
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00003B60 File Offset: 0x00001D60
		internal bool Advance(float work, out Thing finished)
		{
			finished = null;
			DesignationManager designationManager = this.map.designationManager;
			Designation designation = ((this.target == null) ? null : designationManager.DesignationOn(this.target, DesignationDefOf.Deconstruct));
			if (designation == null || (this.activeOrder != null && this.activeOrder != designation))
			{
				this.SetOrder(null);
			}
			Building building;
			if (!this.CanAdvance(this.target, out building) && !this.TrySelectOrder(out building))
			{
				return false;
			}
			if (this.activeOrder == null)
			{
				this.activeOrder = designation;
			}
			this.workDone += work * 1.7f;
			if (this.workDone >= Deconstruction.WorkRequired(building))
			{
				finished = this.target;
				this.SetOrder(null);
			}
			return true;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003C14 File Offset: 0x00001E14
		internal bool CanAdvance(Thing thing, out Building building)
		{
			building = ((thing != null) ? MinifyUtility.GetInnerIfMinified(thing) : null) as Building;
			if (building != null && building.def.destroyable && !building.def.IsFrame && building.def.category == 3 && thing.Spawned && thing.Map == this.map && thing.def.destroyable && !GridsUtility.Fogged(thing.Position, this.map) && !FireUtility.IsBurning(thing) && !ForbidUtility.IsForbidden(thing, Faction.OfPlayer) && !this.map.reservationManager.IsReserved(thing) && !this.map.physicalInteractionReservationManager.IsReserved(thing) && building.DeconstructibleBy(Faction.OfPlayer))
			{
				CompExplosive compExplosive = ThingCompUtility.TryGetComp<CompExplosive>(building);
				return compExplosive == null || !compExplosive.wickStarted;
			}
			return false;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003D1D File Offset: 0x00001F1D
		internal static float WorkRequired(Building building)
		{
			return Mathf.Clamp(StatExtension.GetStatValue(building, StatDefOf.WorkToBuild, true, -1), 20f, 3000f);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003D3C File Offset: 0x00001F3C
		private bool TrySelectOrder(out Building building)
		{
			foreach (Designation designation in this.map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Deconstruct))
			{
				if (this.CanAdvance(designation.target.Thing, out building))
				{
					this.SetOrder(designation);
					return true;
				}
			}
			building = null;
			return false;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003DB8 File Offset: 0x00001FB8
		private void SetOrder(Designation order)
		{
			this.activeOrder = order;
			this.target = ((order != null) ? order.target.Thing : null);
			this.workDone = 0f;
		}

		// Token: 0x0400002D RID: 45
		internal const float WorkRateFactor = 1.7f;

		// Token: 0x0400002E RID: 46
		private readonly Map map;

		// Token: 0x0400002F RID: 47
		private Thing target;

		// Token: 0x04000030 RID: 48
		private Designation activeOrder;

		// Token: 0x04000031 RID: 49
		private float workDone;
	}
}

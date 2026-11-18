using System;
using RimWorld;
using Verse;

namespace AutoBuild
{
	// Token: 0x02000010 RID: 16
	internal sealed class SurfaceRemoval
	{
		// Token: 0x06000056 RID: 86 RVA: 0x00004C41 File Offset: 0x00002E41
		internal SurfaceRemoval(Map map)
		{
			this.map = map;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00004C5C File Offset: 0x00002E5C
		internal void ExposeData()
		{
			Scribe_Values.Look<IntVec3>(ref this.target, "surfaceRemovalTarget", IntVec3.Invalid, false);
			Scribe_Values.Look<RemovalOrder>(ref this.order, "surfaceRemovalOrder", RemovalOrder.Roof, false);
			Scribe_Values.Look<bool>(ref this.clearFloorFirst, "surfaceRemovalClearFloorFirst", false, false);
			Scribe_Values.Look<float>(ref this.workDone, "surfaceRemovalWorkDone", 0f, false);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00004CBC File Offset: 0x00002EBC
		internal bool Advance(float work)
		{
			if (!this.CanAdvance(this.target, this.order) && !this.TrySelectOrder())
			{
				this.target = IntVec3.Invalid;
				this.workDone = 0f;
				return false;
			}
			bool flag = this.order == RemovalOrder.Foundation && this.HasRemovableTopLayer(this.target);
			if (this.clearFloorFirst != flag)
			{
				this.clearFloorFirst = flag;
				this.workDone = 0f;
			}
			float num = ((this.order == RemovalOrder.Roof) ? 65f : ((this.order == RemovalOrder.Floor || this.clearFloorFirst) ? 200f : 300f));
			this.workDone += work * 1.7f;
			if (this.workDone >= num)
			{
				IntVec3 intVec = this.target;
				this.target = IntVec3.Invalid;
				this.workDone = 0f;
				this.Complete(intVec);
			}
			return true;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004DA0 File Offset: 0x00002FA0
		internal bool CanAdvance(IntVec3 cell, RemovalOrder kind)
		{
			if (!cell.IsValid || !GenGrid.InBounds(cell, this.map) || GridsUtility.Fogged(cell, this.map) || this.map.reservationManager.IsReserved(cell) || this.map.physicalInteractionReservationManager.IsReserved(cell))
			{
				return false;
			}
			if (kind == RemovalOrder.Roof)
			{
				if (this.map.areaManager.NoRoof[cell])
				{
					RoofDef roofDef = this.map.roofGrid.RoofAt(cell);
					return roofDef != null && !roofDef.isThickRoof;
				}
				return false;
			}
			else
			{
				if (kind == RemovalOrder.Floor)
				{
					return this.map.designationManager.DesignationAt(cell, DesignationDefOf.RemoveFloor) != null && this.map.terrainGrid.CanRemoveTopLayerAt(cell) && !WorkGiver_ConstructRemoveFloor.AnyBuildingBlockingFloorRemoval(cell, this.map);
				}
				return this.map.designationManager.DesignationAt(cell, DesignationDefOf.RemoveFoundation) != null && this.map.terrainGrid.CanRemoveFoundationAt(cell) && !WorkGiver_ConstructRemoveFoundation.AnyBuildingBlockingFoundationRemoval(cell, this.map);
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004EBF File Offset: 0x000030BF
		private bool HasRemovableTopLayer(IntVec3 cell)
		{
			return this.map.terrainGrid.UnderTerrainAt(cell) != null && this.map.terrainGrid.CanRemoveTopLayerAt(cell);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00004EE8 File Offset: 0x000030E8
		private bool TrySelectOrder()
		{
			foreach (IntVec3 intVec in this.map.areaManager.NoRoof.ActiveCells)
			{
				if (GridsUtility.Roofed(intVec, this.map) && this.CanAdvance(intVec, RemovalOrder.Roof))
				{
					this.SetTarget(intVec, RemovalOrder.Roof);
					return true;
				}
			}
			return this.TrySelectTerrainOrder(RemovalOrder.Floor, DesignationDefOf.RemoveFloor) || this.TrySelectTerrainOrder(RemovalOrder.Foundation, DesignationDefOf.RemoveFoundation);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00004F80 File Offset: 0x00003180
		private bool TrySelectTerrainOrder(RemovalOrder kind, DesignationDef def)
		{
			foreach (Designation designation in this.map.designationManager.SpawnedDesignationsOfDef(def))
			{
				IntVec3 cell = designation.target.Cell;
				if (this.CanAdvance(cell, kind))
				{
					this.SetTarget(cell, kind);
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00004FF4 File Offset: 0x000031F4
		private void SetTarget(IntVec3 cell, RemovalOrder kind)
		{
			this.target = cell;
			this.order = kind;
			this.workDone = 0f;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00005010 File Offset: 0x00003210
		private void Complete(IntVec3 cell)
		{
			if (this.order == RemovalOrder.Roof)
			{
				this.map.roofGrid.SetRoof(cell, null);
				RoofCollapseCellsFinder.CheckCollapseFlyingRoofs(CellRect.SingleCell(cell), this.map, true, false);
				return;
			}
			this.map.snowGrid.SetDepth(cell, 0f);
			SandGrid sandGrid = this.map.sandGrid;
			if (sandGrid != null)
			{
				sandGrid.SetDepth(cell, 0f);
			}
			TerrainGrid terrainGrid = this.map.terrainGrid;
			if (this.order == RemovalOrder.Floor || this.clearFloorFirst)
			{
				TerrainDef terrainDef = terrainGrid.TempTerrainAt(cell);
				bool flag = this.order != RemovalOrder.Foundation || !this.DropFoundationLeavingsNearby(cell, terrainDef ?? terrainGrid.TopTerrainAt(cell));
				if (terrainDef != null && terrainDef.Removable)
				{
					terrainGrid.RemoveTempTerrain(cell, flag, false);
				}
				else
				{
					terrainGrid.RemoveTopLayer(cell, flag);
				}
				this.map.designationManager.TryRemoveDesignation(cell, DesignationDefOf.RemoveFloor);
			}
			else
			{
				bool flag2 = !this.DropFoundationLeavingsNearby(cell, terrainGrid.FoundationAt(cell));
				terrainGrid.RemoveFoundation(cell, flag2);
				this.map.designationManager.TryRemoveDesignation(cell, DesignationDefOf.RemoveFoundation);
			}
			FilthMaker.RemoveAllFilth(cell, this.map);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00005138 File Offset: 0x00003338
		private bool DropFoundationLeavingsNearby(IntVec3 cell, TerrainDef terrain)
		{
			foreach (IntVec3 intVec in GenRadial.RadialCellsAround(cell, 12f, false))
			{
				if (GenGrid.InBounds(intVec, this.map) && !GridsUtility.Fogged(intVec, this.map) && GenGrid.Standable(intVec, this.map) && !FireUtility.ContainsStaticFire(intVec, this.map) && GridsUtility.GetItemCount(intVec, this.map) < GridsUtility.GetMaxItemsAllowedInCell(intVec, this.map) && this.map.designationManager.DesignationAt(intVec, DesignationDefOf.RemoveFoundation) == null)
				{
					GenLeaving.DoLeavingsFor(terrain, intVec, this.map);
					return true;
				}
			}
			return false;
		}

		// Token: 0x0400004A RID: 74
		private const float FloorWork = 200f;

		// Token: 0x0400004B RID: 75
		private const float FoundationWork = 300f;

		// Token: 0x0400004C RID: 76
		private const float RoofWork = 65f;

		// Token: 0x0400004D RID: 77
		private readonly Map map;

		// Token: 0x0400004E RID: 78
		private IntVec3 target = IntVec3.Invalid;

		// Token: 0x0400004F RID: 79
		private RemovalOrder order;

		// Token: 0x04000050 RID: 80
		private bool clearFloorFirst;

		// Token: 0x04000051 RID: 81
		private float workDone;
	}
}

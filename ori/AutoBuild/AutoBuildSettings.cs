using System;
using System.Collections.Generic;
using Verse;

namespace AutoBuild
{
	// Token: 0x02000006 RID: 6
	public sealed class AutoBuildSettings : ModSettings
	{
		// Token: 0x06000021 RID: 33 RVA: 0x00003084 File Offset: 0x00001284
		internal int StockReserve(ThingDef material)
		{
			int num;
			if (!this.stockReserves.TryGetValue(material.defName, out num))
			{
				return 0;
			}
			return num;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000030A9 File Offset: 0x000012A9
		internal void SetStockReserve(ThingDef material, int count)
		{
			if (count > 0)
			{
				this.stockReserves[material.defName] = count;
				return;
			}
			this.stockReserves.Remove(material.defName);
		}

		// Token: 0x06000023 RID: 35 RVA: 0x000030D4 File Offset: 0x000012D4
		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref this.progressionEnabled, "progressionEnabled", true, false);
			Scribe_Values.Look<bool>(ref this.allowMapMaterials, "allowMaterialsOutsideStorage", false, false);
			Scribe_Values.Look<bool>(ref this.allowDuringRaids, "allowDuringRaids", false, false);
			Scribe_Values.Look<bool>(ref this.clearObstacles, "clearObstacles", false, false);
			Scribe_Values.Look<bool>(ref this.autoDeconstruct, "autoDeconstruct", true, false);
			Scribe_Collections.Look<string, int>(ref this.stockReserves, "stockReserves", 1, 1);
			if (this.stockReserves == null)
			{
				this.stockReserves = new Dictionary<string, int>();
			}
		}

		// Token: 0x04000011 RID: 17
		internal bool progressionEnabled = true;

		// Token: 0x04000012 RID: 18
		internal bool allowMapMaterials;

		// Token: 0x04000013 RID: 19
		internal bool allowDuringRaids;

		// Token: 0x04000014 RID: 20
		internal bool clearObstacles;

		// Token: 0x04000015 RID: 21
		internal bool autoDeconstruct = true;

		// Token: 0x04000016 RID: 22
		private Dictionary<string, int> stockReserves = new Dictionary<string, int>();
	}
}

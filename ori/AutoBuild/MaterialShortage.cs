using System;
using Verse;

namespace AutoBuild
{
	// Token: 0x0200000D RID: 13
	internal readonly struct MaterialShortage
	{
		// Token: 0x0600004F RID: 79 RVA: 0x0000474A File Offset: 0x0000294A
		internal MaterialShortage(ThingDef material, int missing, int protectedCount)
		{
			this.Material = material;
			this.Missing = missing;
			this.Protected = protectedCount;
		}

		// Token: 0x0400003A RID: 58
		internal readonly ThingDef Material;

		// Token: 0x0400003B RID: 59
		internal readonly int Missing;

		// Token: 0x0400003C RID: 60
		internal readonly int Protected;
	}
}

using System;
using UnityEngine;
using Verse;

namespace AutoBuild
{
	// Token: 0x02000005 RID: 5
	public sealed class AutoBuildMod : Mod
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600001C RID: 28 RVA: 0x00002EE7 File Offset: 0x000010E7
		// (set) Token: 0x0600001D RID: 29 RVA: 0x00002EEE File Offset: 0x000010EE
		internal static AutoBuildSettings Settings { get; private set; }

		// Token: 0x0600001E RID: 30 RVA: 0x00002EF6 File Offset: 0x000010F6
		public AutoBuildMod(ModContentPack content)
			: base(content)
		{
			AutoBuildMod.Settings = base.GetSettings<AutoBuildSettings>();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002F0A File Offset: 0x0000110A
		public override string SettingsCategory()
		{
			return "AutoBuild";
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002F14 File Offset: 0x00001114
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(inRect);
			listing_Standard.CheckboxLabeled(Translator.Translate("AutoBuild_Progression"), ref AutoBuildMod.Settings.progressionEnabled, Translator.Translate("AutoBuild_ProgressionTooltip"), 0f, 1f);
			listing_Standard.CheckboxLabeled(Translator.Translate("AutoBuild_AllowMapMaterials"), ref AutoBuildMod.Settings.allowMapMaterials, Translator.Translate("AutoBuild_AllowMapMaterialsTooltip"), 0f, 1f);
			listing_Standard.CheckboxLabeled(Translator.Translate("AutoBuild_AllowDuringRaids"), ref AutoBuildMod.Settings.allowDuringRaids, Translator.Translate("AutoBuild_AllowDuringRaidsTooltip"), 0f, 1f);
			listing_Standard.CheckboxLabeled(Translator.Translate("AutoBuild_ClearObstacles"), ref AutoBuildMod.Settings.clearObstacles, Translator.Translate("AutoBuild_ClearObstaclesTooltip"), 0f, 1f);
			listing_Standard.CheckboxLabeled(Translator.Translate("AutoBuild_AutoDeconstruct"), ref AutoBuildMod.Settings.autoDeconstruct, Translator.Translate("AutoBuild_AutoDeconstructTooltip"), 0f, 1f);
			listing_Standard.Gap(12f);
			if (listing_Standard.ButtonText(Translator.Translate("AutoBuild_StockReserves"), null, 1f))
			{
				Find.WindowStack.Add(new StockReserveWindow(this));
			}
			listing_Standard.End();
		}
	}
}

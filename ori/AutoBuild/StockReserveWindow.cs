using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AutoBuild
{
	// Token: 0x0200000E RID: 14
	internal sealed class StockReserveWindow : Window
	{
		// Token: 0x06000050 RID: 80 RVA: 0x00004764 File Offset: 0x00002964
		internal StockReserveWindow(AutoBuildMod mod)
			: base(null)
		{
			this.mod = mod;
			this.doCloseX = true;
			this.absorbInputAroundWindow = true;
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
			{
				if (thingDef.category == 2 && thingDef.EverHaulable && !thingDef.IsCorpse)
				{
					this.materials.Add(thingDef);
				}
			}
			this.materials.Sort(delegate(ThingDef a, ThingDef b)
			{
				bool flag = AutoBuildMod.Settings.StockReserve(a) > 0;
				bool flag2 = AutoBuildMod.Settings.StockReserve(b) > 0;
				if (flag == flag2)
				{
					return StringComparer.CurrentCultureIgnoreCase.Compare(a.label, b.label);
				}
				return flag2.CompareTo(flag);
			});
			this.Filter();
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000051 RID: 81 RVA: 0x0000484C File Offset: 0x00002A4C
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(600f, 600f);
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00004860 File Offset: 0x00002A60
		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = 2;
			Widgets.Label(new Rect(0f, 0f, inRect.width - 32f, 32f), Translator.Translate("AutoBuild_StockReserves"));
			Text.Font = 1;
			Widgets.Label(new Rect(0f, 40f, inRect.width, 64f), Translator.Translate("AutoBuild_StockReserveHint"));
			Widgets.Label(new Rect(0f, 115f, 122f, 26f), Translator.Translate("AutoBuild_StockSearch"));
			string text = Widgets.TextField(new Rect(122f, 112f, inRect.width - 122f, 28f), this.search);
			if (text != this.search)
			{
				this.search = text;
				this.scroll = Vector2.zero;
				this.Filter();
			}
			if (this.matches.Count == 0)
			{
				Widgets.Label(new Rect(0f, 155f, inRect.width, 28f), Translator.Translate("AutoBuild_StockNoMatches"));
				return;
			}
			Rect rect;
			rect..ctor(0f, 155f, inRect.width, inRect.height - 155f);
			Rect rect2;
			rect2..ctor(0f, 0f, rect.width - 18f, (float)this.matches.Count * 32f);
			Widgets.BeginScrollView(rect, ref this.scroll, rect2, true);
			try
			{
				int num = Mathf.Clamp(Mathf.FloorToInt(this.scroll.y / 32f), 0, this.matches.Count - 1);
				int num2 = Mathf.Min(this.matches.Count, Mathf.CeilToInt((this.scroll.y + rect.height) / 32f));
				for (int i = num; i < num2; i++)
				{
					this.DrawMaterial(new Rect(0f, (float)i * 32f, rect2.width, 32f), this.matches[i], i);
				}
			}
			finally
			{
				Widgets.EndScrollView();
			}
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00004A9C File Offset: 0x00002C9C
		public override void PostClose()
		{
			base.PostClose();
			if (this.changed)
			{
				this.mod.WriteSettings();
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00004AB8 File Offset: 0x00002CB8
		private void Filter()
		{
			string text = this.search.Trim();
			this.matches.Clear();
			foreach (ThingDef thingDef in this.materials)
			{
				if ((thingDef.label ?? thingDef.defName).IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 || thingDef.defName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					this.matches.Add(thingDef);
				}
			}
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00004B54 File Offset: 0x00002D54
		private void DrawMaterial(Rect row, ThingDef material, int index)
		{
			if (index % 2 == 0)
			{
				Widgets.DrawLightHighlight(row);
			}
			Rect rect = new Rect(row.x + 6f, row.y + 5f, row.width - 126f, 26f);
			Widgets.Label(rect, material.LabelCap);
			TooltipHandler.TipRegion(rect, material.defName);
			int num = AutoBuildMod.Settings.StockReserve(material);
			int num2 = num;
			string text;
			this.buffers.TryGetValue(material, out text);
			GUI.SetNextControlName(material.defName);
			Widgets.TextFieldNumeric<int>(new Rect(row.xMax - 112f, row.y + 2f, 106f, 27f), ref num2, ref text, 0f, 1E+09f);
			this.buffers[material] = text;
			if (num2 != num)
			{
				AutoBuildMod.Settings.SetStockReserve(material, num2);
				this.changed = true;
			}
		}

		// Token: 0x0400003D RID: 61
		private const float RowHeight = 32f;

		// Token: 0x0400003E RID: 62
		private const int MaxReserve = 1000000000;

		// Token: 0x0400003F RID: 63
		private readonly AutoBuildMod mod;

		// Token: 0x04000040 RID: 64
		private readonly List<ThingDef> materials = new List<ThingDef>();

		// Token: 0x04000041 RID: 65
		private readonly List<ThingDef> matches = new List<ThingDef>();

		// Token: 0x04000042 RID: 66
		private readonly Dictionary<ThingDef, string> buffers = new Dictionary<ThingDef, string>();

		// Token: 0x04000043 RID: 67
		private Vector2 scroll;

		// Token: 0x04000044 RID: 68
		private string search = string.Empty;

		// Token: 0x04000045 RID: 69
		private bool changed;
	}
}

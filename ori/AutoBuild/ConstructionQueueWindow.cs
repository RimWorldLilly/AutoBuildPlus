using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AutoBuild
{
	// Token: 0x02000009 RID: 9
	internal sealed class ConstructionQueueWindow : Window
	{
		// Token: 0x06000037 RID: 55 RVA: 0x000035D0 File Offset: 0x000017D0
		internal ConstructionQueueWindow(AutoBuildMap construction)
			: base(null)
		{
			this.construction = construction;
			this.materials = new StockMaterials(construction.Map);
			this.doCloseX = true;
			this.draggable = true;
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000038 RID: 56 RVA: 0x0000362B File Offset: 0x0000182B
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(740f, 560f);
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x0000363C File Offset: 0x0000183C
		public override void DoWindowContents(Rect inRect)
		{
			if (Find.CurrentMap != this.construction.Map)
			{
				this.Close(true);
				return;
			}
			if (Time.realtimeSinceStartup >= this.nextRefresh)
			{
				this.Refresh();
			}
			Text.Font = 2;
			Widgets.Label(new Rect(0f, 0f, inRect.width - 32f, 32f), Translator.Translate("AutoBuild_QueueTitle"));
			Text.Font = 1;
			Widgets.Label(new Rect(0f, 38f, inRect.width, 42f), this.notice);
			if (this.queue.Count == 0)
			{
				Widgets.Label(new Rect(0f, 84f, inRect.width, 32f), Translator.Translate("AutoBuild_QueueEmpty"));
				return;
			}
			Rect rect;
			rect..ctor(0f, 84f, inRect.width, inRect.height - 84f);
			Rect rect2;
			rect2..ctor(0f, 0f, rect.width - 18f, (float)this.queue.Count * 34f);
			Widgets.BeginScrollView(rect, ref this.scroll, rect2, true);
			try
			{
				int num = Mathf.Clamp(Mathf.FloorToInt(this.scroll.y / 34f), 0, this.queue.Count - 1);
				int num2 = Mathf.Min(this.queue.Count, Mathf.CeilToInt((this.scroll.y + rect.height) / 34f));
				for (int i = num; i < num2; i++)
				{
					Thing thing = this.queue[i];
					if (thing.Spawned)
					{
						this.DrawProject(new Rect(0f, (float)i * 34f, rect2.width, 34f), thing, i);
					}
				}
			}
			finally
			{
				Widgets.EndScrollView();
				this.materials.Clear();
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003838 File Offset: 0x00001A38
		private void Refresh()
		{
			this.nextRefresh = Time.realtimeSinceStartup + 0.5f;
			this.notice = this.construction.QueueNotice();
			this.construction.CollectProjects(this.projects);
			this.queue.Clear();
			foreach (Thing thing in this.projects)
			{
				if (this.construction.IsUrgent(thing))
				{
					this.queue.Add(thing);
				}
			}
			foreach (Thing thing2 in this.projects)
			{
				if (!this.construction.IsUrgent(thing2))
				{
					this.queue.Add(thing2);
				}
			}
			this.statuses.Clear();
		}

		// Token: 0x0600003B RID: 59 RVA: 0x0000393C File Offset: 0x00001B3C
		private void DrawProject(Rect row, Thing project, int index)
		{
			if (index % 2 == 0)
			{
				Widgets.DrawLightHighlight(row);
			}
			Rect rect;
			rect..ctor(row.xMax - 114f, row.y + 3f, 110f, 28f);
			bool flag = this.construction.IsUrgent(project);
			if (Widgets.ButtonText(rect, Translator.Translate(flag ? "AutoBuild_QueueUrgentOn" : "AutoBuild_QueueUrgent"), true, true, true, null))
			{
				this.construction.ToggleUrgent(project);
				this.nextRefresh = 0f;
			}
			TooltipHandler.TipRegion(rect, Translator.Translate(flag ? "AutoBuild_QueueRemoveUrgent" : "AutoBuild_QueueSetUrgent"));
			Rect rect2;
			rect2..ctor(row.x + 4f, row.y + 3f, row.width * 0.38f, 28f);
			string labelCap = project.LabelCap;
			if (Widgets.ButtonText(rect2, GenText.Truncate(labelCap, rect2.width - 8f, null), false, true, true, new TextAnchor?(3)))
			{
				CameraJumper.TryJumpAndSelect(project, 0);
			}
			TooltipHandler.TipRegion(rect2, TranslatorFormattedStringExtensions.Translate("AutoBuild_QueueLocate", labelCap, project.Position.x, project.Position.z));
			string text;
			if (!this.statuses.TryGetValue(project, out text))
			{
				text = this.construction.ProjectStatus(project, this.materials);
				this.statuses.Add(project, text);
			}
			Rect rect3;
			rect3..ctor(rect2.xMax + 8f, row.y + 7f, rect.x - rect2.xMax - 16f, 24f);
			Widgets.Label(rect3, GenText.Truncate(text, rect3.width, null));
			TooltipHandler.TipRegion(rect3, text);
		}

		// Token: 0x04000023 RID: 35
		private const float RowHeight = 34f;

		// Token: 0x04000024 RID: 36
		private const float RefreshInterval = 0.5f;

		// Token: 0x04000025 RID: 37
		private readonly AutoBuildMap construction;

		// Token: 0x04000026 RID: 38
		private readonly StockMaterials materials;

		// Token: 0x04000027 RID: 39
		private readonly List<Thing> projects = new List<Thing>();

		// Token: 0x04000028 RID: 40
		private readonly List<Thing> queue = new List<Thing>();

		// Token: 0x04000029 RID: 41
		private readonly Dictionary<Thing, string> statuses = new Dictionary<Thing, string>();

		// Token: 0x0400002A RID: 42
		private Vector2 scroll;

		// Token: 0x0400002B RID: 43
		private float nextRefresh;

		// Token: 0x0400002C RID: 44
		private string notice;
	}
}

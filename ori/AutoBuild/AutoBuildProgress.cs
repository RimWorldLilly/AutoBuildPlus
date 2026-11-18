using System;
using System.Globalization;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoBuild
{
	// Token: 0x02000007 RID: 7
	public sealed class AutoBuildProgress : GameComponent
	{
		// Token: 0x06000025 RID: 37 RVA: 0x00003181 File Offset: 0x00001381
		public AutoBuildProgress(Game game)
		{
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00003189 File Offset: 0x00001389
		internal static AutoBuildProgress Current
		{
			get
			{
				return global::Verse.Current.Game.GetComponent<AutoBuildProgress>();
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000027 RID: 39 RVA: 0x00003195 File Offset: 0x00001395
		private static bool ProgressionEnabled
		{
			get
			{
				return AutoBuildMod.Settings.progressionEnabled;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000028 RID: 40 RVA: 0x000031A1 File Offset: 0x000013A1
		internal int SpeedLevel
		{
			get
			{
				if (!AutoBuildProgress.ProgressionEnabled)
				{
					return 40;
				}
				return this.LevelAt(AutoBuildProgress.SpeedThresholds);
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000029 RID: 41 RVA: 0x000031B8 File Offset: 0x000013B8
		internal int QualityLevel
		{
			get
			{
				if (!AutoBuildProgress.ProgressionEnabled)
				{
					return 20;
				}
				return this.LevelAt(AutoBuildProgress.QualityThresholds);
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600002A RID: 42 RVA: 0x000031CF File Offset: 0x000013CF
		internal float WorkPerTick
		{
			get
			{
				return (0.3f + (float)this.SpeedLevel * 0.0875f) * 1.7f * 0.5f;
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000031F0 File Offset: 0x000013F0
		internal void AddWork(float work)
		{
			if (AutoBuildProgress.ProgressionEnabled && work > 0f)
			{
				this.experience = Math.Min(this.experience + (double)work, AutoBuildProgress.SpeedThresholds[39]);
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003220 File Offset: 0x00001420
		internal QualityCategory RollQuality()
		{
			if (!AutoBuildProgress.ProgressionEnabled)
			{
				return 6;
			}
			int qualityLevel = this.QualityLevel;
			bool flag = qualityLevel >= 20 && Rand.Chance(0.1f);
			QualityCategory qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(qualityLevel, flag);
			if (qualityLevel >= 15)
			{
				qualityCategory = (byte)Math.Min(qualityCategory + 1, 6);
			}
			return qualityCategory;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003268 File Offset: 0x00001468
		internal string Describe()
		{
			int speedLevel = this.SpeedLevel;
			string text = TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressExperience", AutoBuildProgress.FormatExperience((int)Math.Floor(this.experience)));
			if (!AutoBuildProgress.ProgressionEnabled)
			{
				return TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressLegendaryMode", speedLevel) + "\n" + text;
			}
			int qualityLevel = this.QualityLevel;
			string text2 = TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressLevels", speedLevel, qualityLevel);
			string text3 = TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressSpeedNext", this.DescribeNextLevel(speedLevel, AutoBuildProgress.SpeedThresholds));
			string text4 = TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressQualityNext", this.DescribeNextLevel(qualityLevel, AutoBuildProgress.QualityThresholds));
			string text5 = string.Concat(new string[] { text2, "\n", text, "\n", text3, "\n", text4 });
			if (qualityLevel >= 15)
			{
				text5 += "\n" + Translator.Translate("AutoBuild_ProgressSpecialist");
			}
			if (qualityLevel >= 20)
			{
				text5 += "\n" + TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressInspired", Mathf.RoundToInt(10f));
			}
			return text5;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000033C8 File Offset: 0x000015C8
		private int LevelAt(double[] thresholds)
		{
			int num = 1;
			while (num < thresholds.Length && this.experience >= thresholds[num])
			{
				num++;
			}
			return num;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000033F0 File Offset: 0x000015F0
		private string DescribeNextLevel(int level, double[] thresholds)
		{
			if (level >= thresholds.Length)
			{
				return Translator.Translate("AutoBuild_ProgressMax");
			}
			double num = thresholds[level - 1];
			double num2 = thresholds[level];
			int num3 = (int)Math.Ceiling(num2 - this.experience);
			int num4 = Mathf.Clamp((int)Math.Floor(100.0 * (this.experience - num) / (num2 - num)), 0, 99);
			return TranslatorFormattedStringExtensions.Translate("AutoBuild_ProgressRemaining", level + 1, AutoBuildProgress.FormatExperience(num3), num4);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x0000347C File Offset: 0x0000167C
		private static double[] CreateSpeedThresholds()
		{
			double[] array = new double[40];
			Array.Copy(AutoBuildProgress.BaseThresholds, array, AutoBuildProgress.BaseThresholds.Length);
			double num = AutoBuildProgress.BaseThresholds[19] - AutoBuildProgress.BaseThresholds[18];
			for (int i = AutoBuildProgress.BaseThresholds.Length; i < array.Length; i++)
			{
				num += 15000.0;
				array[i] = array[i - 1] + num;
			}
			return array;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000034E0 File Offset: 0x000016E0
		private static double[] CreateQualityThresholds()
		{
			double[] array = new double[20];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = AutoBuildProgress.BaseThresholds[i] / 0.6;
			}
			return array;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003518 File Offset: 0x00001718
		private static string FormatExperience(int amount)
		{
			return amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '\u00a0');
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003537 File Offset: 0x00001737
		public override void ExposeData()
		{
			Scribe_Values.Look<double>(ref this.experience, "experience", 0.0, false);
		}

		// Token: 0x04000017 RID: 23
		private const int MaxSpeedLevel = 40;

		// Token: 0x04000018 RID: 24
		private const int MaxQualityLevel = 20;

		// Token: 0x04000019 RID: 25
		private const int SpecialistLevel = 15;

		// Token: 0x0400001A RID: 26
		private const float InspirationChance = 0.1f;

		// Token: 0x0400001B RID: 27
		private const double QualityExperienceFactor = 0.6;

		// Token: 0x0400001C RID: 28
		private const int AdvancedSpeedCostIncrease = 15000;

		// Token: 0x0400001D RID: 29
		private const float AutomaticWorkFactor = 0.5f;

		// Token: 0x0400001E RID: 30
		private static readonly double[] BaseThresholds = new double[]
		{
			0.0, 120.0, 350.0, 750.0, 1350.0, 2250.0, 3500.0, 9000.0, 17000.0, 30000.0,
			50000.0, 80000.0, 120000.0, 175000.0, 245000.0, 340000.0, 460000.0, 610000.0, 790000.0, 1000000.0
		};

		// Token: 0x0400001F RID: 31
		private static readonly double[] SpeedThresholds = AutoBuildProgress.CreateSpeedThresholds();

		// Token: 0x04000020 RID: 32
		private static readonly double[] QualityThresholds = AutoBuildProgress.CreateQualityThresholds();

		// Token: 0x04000021 RID: 33
		private double experience;
	}
}

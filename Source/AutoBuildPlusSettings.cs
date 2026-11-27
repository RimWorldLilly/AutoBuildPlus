using System;
using Verse;

namespace AutoBuildInstantConstruction
{
    public sealed class AutoBuildPlusSettings : ModSettings
    {
        internal int updateInterval = 1;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref updateInterval, "updateInterval", 1);
            updateInterval = Math.Max(1, Math.Min(15, updateInterval));
        }
    }
}

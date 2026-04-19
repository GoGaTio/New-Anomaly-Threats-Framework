using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;
using UnityEngine;
using System.Diagnostics;

namespace NAT
{
	public class NewAnomalyThreatsSettings : ModSettings
    {
		public bool allowEndGameRaid = true;

		public float endGameRaidChanceFactor = 1f;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref allowEndGameRaid, "allowEndGameRaid", true);
			base.ExposeData();
		}
	}

	public class NewAnomalyThreatsMod : Mod
	{

		NewAnomalyThreatsSettings settings;

		public NewAnomalyThreatsMod(ModContentPack content) : base(content)
		{
			this.settings = GetSettings<NewAnomalyThreatsSettings>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);
			listingStandard.CheckboxLabeled("NAT_Setting_AllowRaid".Translate(), ref settings.allowEndGameRaid, "NAT_Setting_AllowRaid_Desc".Translate());
			listingStandard.End();
			base.DoSettingsWindowContents(inRect);
		}
		public override string SettingsCategory()
		{
			return "New Anomaly Threats";
		}
	}
}

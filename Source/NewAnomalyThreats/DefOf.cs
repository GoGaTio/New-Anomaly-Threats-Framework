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
	[DefOf]
	public static class NATColorDefOf
	{
		public static ColorDef Structure_Red;

		public static ColorDef Structure_Blue;

		public static ColorDef Structure_Green;
	}

	[DefOf]
	public static class NATDefOf
	{
		public static ThingDef NAT_SignalAction_Sightstealers;

		public static ThingDef NAT_ContainmentDoor;

		public static ThingDef NAT_AncientHoldingPlatform;

		public static ThingDef NAT_AncientElectricInhibitor;

		public static ThingDef NAT_HoraxMachine_Crashed;

		public static ThingDef NAT_HoraxStabilizer;

		public static ThingDef NAT_HoraxMachineIncoming;

		public static PawnKindDef NAT_Researcher;

		public static JobDef NAT_Seal;

		public static JobDef NAT_InteractWithVault;

		public static JobDef NAT_BringAdditionalOfferings;

		public static JobDef NAT_MoveEntity;

		public static HediffDef NAT_InducedPain;

		public static HediffDef NAT_Subdued;

		[MayRequire("GoGaTio.NewAnomalyThreats.SeaMonsters")]
		public static HediffDef NAT_BilePowerSerum;

		public static HediffDef NAT_SlowedByBile;

		public static HediffDef NAT_EmotionSuppression;

		[MayRequire("GoGaTio.NewAnomalyThreats.SeaMonsters")]
		public static PawnGroupKindDef NAT_Serpents;

		[MayRequire("GoGaTio.NewAnomalyThreats.SeaMonsters")]
		public static DutyDef NAT_SerpentAssault;

		public static DutyDef NAT_BringAdditionalOfferingsForPsychicRitual;

		public static ThoughtDef NAT_ObeliskSuppression;

		public static MentalBreakDef TerrifyingHallucinations;

        public static SoundDef GestatorGlassShattered;

		public static GenStepDef NAT_UndergroundLayout;

		public static HediffDef NAT_Nail;

		public static DamageDef NociosphereVaporize;
	}
}

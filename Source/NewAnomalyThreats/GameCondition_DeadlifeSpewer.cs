using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using DelaunatorSharp;
using Gilzoide.ManagedJobs;
using Ionic.Crc;
using Ionic.Zlib;
using JetBrains.Annotations;
using KTrie;
using LudeonTK;
using NVorbis.NAudioSupport;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using RimWorld.Utility;
using RuntimeAudioClipLoader;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;

namespace NAT
{
	public class GameCondition_DeadlifeSpewer : GameCondition
	{
		private static readonly IntRange ResurrectIntervalRangeSuccess = new IntRange(60, 300);

		private static readonly IntRange ResurrectIntervalRange = new IntRange(2500, 5000);

		private SkyColorSet skyColorsDay = new SkyColorSet(new Color(0.482f, 0.603f, 0.682f), new Color(0.92f, 0.92f, 0.92f), new Color(0.25f, 0.2f, 0.2f), 0.5f);

		private SkyColorSet skyColorsNight = new SkyColorSet(new Color(0.35f, 0.40f, 0.45f), new Color(0.92f, 0.92f, 0.92f), new Color(0.15f, 0.1f, 0.1f), 0.5f);

		private int resurrectionTicksLeft;

		private Lord lord;

		private readonly List<SkyOverlay> overlays = new List<SkyOverlay>
		{
			new WeatherOverlay_DeathpallFog(),
			new WeatherOverlay_DeathpallAshes()
		};

		public override int TransitionTicks => 7500;

		public override void Init()
		{
			base.Init();
			resurrectionTicksLeft = TransitionTicks;
		}

		public override void GameConditionTick()
		{
			List<Map> affectedMaps = base.AffectedMaps;
			resurrectionTicksLeft--;
			if(resurrectionTicksLeft <= 0)
			{
				Pawn shambler = null;
				for (int i = 0; i < affectedMaps.Count; i++)
				{
					foreach (Thing item in affectedMaps[i].listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
					{
						if (item is Corpse corpse && MutantUtility.CanResurrectAsShambler(corpse) && corpse.Age >= 2500)
						{
							shambler = ResurrectPawn(corpse);
							if (!shambler.Position.Fogged(affectedMaps[i]))
							{
								Messages.Message("DeathPallResurrectedMessage".Translate(shambler), shambler, MessageTypeDefOf.NegativeEvent, historical: false);
							}
							if(lord == null)
							{
								lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_ShamblerAssault(), affectedMaps[i], Gen.YieldSingle(shambler));
							}
							else if(lord.CanAddPawn(shambler))
							{
								lord.AddPawn(shambler);
							}
							break;
						}
					}
				}
				if (shambler == null)
				{
					resurrectionTicksLeft = ResurrectIntervalRange.RandomInRange;
				}
				else
				{
					resurrectionTicksLeft = ResurrectIntervalRangeSuccess.RandomInRange;
				}
			}
			for (int j = 0; j < overlays.Count; j++)
			{
				for (int k = 0; k < affectedMaps.Count; k++)
				{
					overlays[j].TickOverlay(affectedMaps[k], 1f);
				}
			}
		}

		private Pawn ResurrectPawn(Corpse corpse)
		{
			Pawn innerPawn = corpse.InnerPawn;
			MutantUtility.ResurrectAsShambler(innerPawn, 60000, Faction.OfEntities);
			return innerPawn;
		}

		public override void GameConditionDraw(Map map)
		{
			for (int i = 0; i < overlays.Count; i++)
			{
				overlays[i].DrawOverlay(map);
			}
		}

		public override float SkyTargetLerpFactor(Map map)
		{
			return GameConditionUtility.LerpInOutValue(this, TransitionTicks, 1f);
		}

		public override SkyTarget? SkyTarget(Map map)
		{
			float num = GenCelestial.CurCelestialSunGlow(map);
			SkyTarget result = new SkyTarget
			{
				glow = Math.Min(num, 1f),
				colors = SkyColorSet.Lerp(skyColorsNight, skyColorsDay, num)
			};
			if (GenCelestial.IsDaytime(num))
			{
				result.lightsourceShineIntensity = 1f;
				result.lightsourceShineSize = 1f;
			}
			else
			{
				result.lightsourceShineIntensity = 0.7f;
				result.lightsourceShineSize = 0.5f;
			}
			return result;
		}

		public override bool AllowEnjoyableOutsideNow(Map map)
		{
			return false;
		}

		public override List<SkyOverlay> SkyOverlays(Map map)
		{
			return overlays;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref resurrectionTicksLeft, "resurrectionTicksLeft", 0);
		}
	}

	/*public class GameCondition_DeadlifeSpewer : GameCondition
	{
		

		private int nextResurrectTick;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref nextResurrectTick, "nextResurrectTick", 0);
		}

		public override void Init()
		{
			base.Init();
			nextResurrectTick = Find.TickManager.TicksGame + ResurrectIntervalRange.RandomInRange;
		}

		public override void GameConditionTick()
		{
			bool b = false;
			if (Find.TickManager.TicksGame < nextResurrectTick || Find.TickManager.TicksGame % 60 != 0)
			{
				b = true;
			}
			List<Pawn> shamblers = new List<Pawn>();
			List<Map> affectedMaps = base.AffectedMaps;
			for (int i = 0; i < affectedMaps.Count; i++)
			{
				for (int j = 0; j < overlays.Count; j++)
				{
					overlays[j].TickOverlay(affectedMaps[i], 1f);
				}
				if (!b)
				{
					
				}
			}
		}

		private Pawn ResurrectPawn(Corpse corpse)
		{
			Pawn innerPawn = corpse.InnerPawn;
			MutantUtility.ResurrectAsShambler(innerPawn, 120000, Faction.OfEntities);
			return innerPawn;
		}

		public override void End()
		{
			Find.LetterStack.ReceiveLetter("LetterLabelDeathPallEnded".Translate(), "LetterDeathPallEnded".Translate(), LetterDefOf.NeutralEvent);
			base.End();
			base.SingleMap.weatherDecider.StartNextWeather();
		}

		private List<SkyOverlay> overlays = new List<SkyOverlay>
		{
			new WeatherOverlay_DeathpallFog(),
			new WeatherOverlay_DeathpallAshes()
		};

		public override void GameConditionDraw(Map map)
		{
			for (int i = 0; i < overlays.Count; i++)
			{
				overlays[i].DrawOverlay(map);
			}
		}

		public override List<SkyOverlay> SkyOverlays(Map map)
		{
			return overlays;
		}
	}*/
}
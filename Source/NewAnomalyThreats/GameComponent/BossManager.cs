using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static System.Net.Mime.MediaTypeNames;

namespace NAT
{
	public class AnomalyBossManager : IExposable
	{
		public class AnomalyBoss : IExposable
		{
			public AnomalyBoss() { }

			public int timesSummoned = 0;

			public AnomalyBossDef def;

			public List<PawnKindDef> escorts = null;

			public List<PawnKindDefCount> cachedEscorts = new List<PawnKindDefCount>();

			public float lastPoints = -1;

			public int lastSummonedTick = -999999;

			public bool incoming = false;

			public sbyte mapIndex = -1;

			public Map Map
			{
				get
				{
					if (mapIndex >= 0)
					{
						return Find.Maps?[mapIndex];
					}
					return null;
				}
			}

			public List<PawnKindDefCount> GetEscorts(Map map) => GetEscorts(map, StorytellerUtility.DefaultThreatPointsNow(map));

			public List<PawnKindDefCount> GetEscorts(Map map, float points)
			{
				float processedPoints = def.ProcessPoints(points, map);
				if (escorts.NullOrEmpty() || Mathf.Abs(processedPoints - lastPoints) > Mathf.Min(lastPoints, processedPoints) * 0.1f)
				{
					escorts = def.GetBosses(timesSummoned, points);
					escorts.AddRange(GeneratePawns(processedPoints));
					lastPoints = points;
					ReCacheEscorts();
				}
				return cachedEscorts;
			}

			public IEnumerable<PawnKindDef> GeneratePawns(float points)
			{
				var group = def.GetBossGroup(timesSummoned, points);
				float num = points;
				while(num > 0)
				{
					PawnKindDef kind = group.escorts.RandomElementByWeight(x => x.selectionWeight).kind;
					num -= kind.combatPower;
					yield return kind;
				}
			}

			public void Tick()
			{
				if (incoming)
				{
					def.ArriveOnMap(this);
					incoming = false;
				}
			}

			public void ReCacheEscorts()
			{
				cachedEscorts = new List<PawnKindDefCount>();
				foreach(PawnKindDef def in escorts)
				{
					PawnKindDefCount item = cachedEscorts.FirstOrDefault(x => x.kindDef == def);
					if (item == null)
					{
						cachedEscorts.Add(new PawnKindDefCount() { count = 1, kindDef = def });
					}
					else
					{
						item.count++;
					}
				}
			}

			public virtual void ExposeData()
			{
				Scribe_Values.Look(ref incoming, "incoming", defaultValue: false);
				Scribe_Values.Look(ref lastSummonedTick, "lastSummonedTick", -999999);
				Scribe_Values.Look(ref timesSummoned, "timesSummoned", 0);
				Scribe_Values.Look(ref lastPoints, "lastPoints", -1);
				Scribe_Values.Look<sbyte>(ref mapIndex, "mapIndex", -1);
				Scribe_Collections.Look(ref escorts, "escorts", LookMode.Def);
			}
		}

		public AnomalyBossManager()
		{

		}

		public List<AnomalyBoss> bosses = new List<AnomalyBoss>();

		public AcceptanceReport CanCallBoss(AnomalyBossDef def, Map map)
		{
			AnomalyBoss boss = bosses.FirstOrDefault(x => x.def == def);
			int cooldown = Find.TickManager.TicksGame - boss.lastSummonedTick;
			if (cooldown < def.ticksCooldown)
			{
				return "CooldownTime".Translate() + ": " + (def.ticksCooldown - cooldown).ToStringTicksToPeriod();
			}
			return true;
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref bosses, "bosses", LookMode.Deep);
			if(Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				foreach(AnomalyBoss boss in bosses.ToList())
				{
					if(boss.def == null)
					{
						bosses.Remove(boss);
					}
					else if(boss.escorts.NullOrEmpty() || boss.escorts.Any(x => x is null))
					{
						boss.escorts = null;
					}
				}
				InitBosses();
			}
		}

		public List<PawnKindDefCount> GetBossEscorts(AnomalyBossDef def, Map map)
		{
			return bosses.FirstOrDefault(x => x.def == def).GetEscorts(map);
		}

		public string BossWaveComposition(AnomalyBossDef def, Map map)
		{
			string s = def.description + "\n\n" + "NAT_BossGroup" + ":";
			AnomalyBoss boss = bosses.FirstOrDefault(x => x.def == def);
			float points = StorytellerUtility.DefaultThreatPointsNow(map);
			List<PawnKindDefCount> list = boss.GetEscorts(map, points);
			foreach(PawnKindDefCount item in list)
			{
				s += "\n" + "  - " + GenLabel.BestKindLabel(item.kindDef, Gender.None).CapitalizeFirst() + " x" + item.count;
			}
			return s;
		}

		public void Tick()
		{
			for (int num = bosses.Count - 1; num >= 0; num--)
			{
				bosses[num].Tick();
			}
		}

		public void InitBosses()
		{
			foreach (AnomalyBossDef bossDef in DefDatabase<AnomalyBossDef>.AllDefs)
			{
				if (!bosses.Any(x => x.def == bossDef))
				{
					bosses.Add(new AnomalyBoss() { def = bossDef });
				}
			}
		}

		public void CallBoss(AnomalyBossDef def, Map map)
		{
			AnomalyBoss boss = bosses.FirstOrDefault(x => x.def == def);
			boss.lastSummonedTick = Find.TickManager.TicksGame;
			boss.incoming = true;
			boss.mapIndex = (sbyte)map.Index;
		}
	}
}

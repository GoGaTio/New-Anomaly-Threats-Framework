using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace NAT
{
	public class RoomPart_DeadResearcher : RoomPartWorker
	{
		public RoomPart_DeadResearcher(RoomPartDef def)
			: base(def)
		{
		}

		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
		{
			Tool tool = null;
			float value = Rand.Value;
			float age = 3000f + (6000f * Rand.ValueAsync(Find.TickManager.TicksGame));
			if (value > 0.8f)
			{
				tool = PawnKindDefOf.Sightstealer.race.tools[0];
			}
			else if (value > 0.55f)
			{
				tool = PawnKindDefOf.Metalhorror.race.tools[0];
			}
			else if (value > 0.35f)
			{
				tool = PawnKindDefOf.Chimera.race.tools[0];
			}
			else if (value > 0.25f)
			{
				tool = PawnKindDefOf.Trispike.race.tools[0];
			}
			else if (value > 0.1f)
			{
				tool = PawnKindDefOf.Fingerspike.race.tools[0];
			}
			else
			{
				tool = PawnKindDefOf.Toughspike.race.tools[0];
			}
			RoomGenUtility.SpawnCorpses(room, map, IntRange.One, NATDefOf.NAT_Researcher, tool.capacities.RandomElement().VerbsProperties.RandomElement().meleeDamageDef, null, tool, new FloatRange(age - 1f, age + 1f));
		}
	}

	public class RoomPart_PreDoorThingDef : RoomPartDef
	{
		public ThingDef thingDef;

		public ThingDef stuffDef;

		public int offset = 2;

		public float chancePerDoor = 1f;

		public IntRange? countRange = null;

		public RoomPart_PreDoorThingDef()
		{
			workerClass = typeof(RoomPart_PreDoorThing);
		}
	}

	public class RoomPart_PreDoorThing : RoomPartWorker
	{
		public new RoomPart_PreDoorThingDef def => (RoomPart_PreDoorThingDef)base.def;

		public RoomPart_PreDoorThing(RoomPartDef def)
			: base(def)
		{
		}

		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
		{
			SpawnDoorBarricades(def.thingDef, room, map, faction, def.chancePerDoor, def.stuffDef, def.offset, def.countRange?.RandomInRange);
		}

		public static void SpawnDoorBarricades(ThingDef def, LayoutRoom room, Map map, Faction faction = null, float chancePerDoor = 1f, ThingDef stuff = null, int offset = 2, int? count = null)
		{
			if (chancePerDoor <= 0f)
			{
				return;
			}
			if (count != null && count.Value < 1)
			{
				return;
			}
			List<(IntVec3, Rot4)> list = new List<(IntVec3, Rot4)>();
			foreach (CellRect rect in room.rects)
			{
				foreach (IntVec3 edgeCell in rect.EdgeCells)
				{
					if (edgeCell.GetDoor(map) != null)
					{
						list.Add((edgeCell, rect.GetEdgeCellRot(edgeCell)));
					}
				}
			}

			if (count == null)
			{
				count = list.Count;
			}
			else
			{
				count = Mathf.Min(list.Count, count.Value);
			}
			for (int i = 1; i < count.Value; i++)
			{
				if (Rand.Chance(chancePerDoor))
				{
					var item = list.RandomElement();
					list.Remove(item);
					IntVec3 pos = item.Item1 + item.Item2.Opposite.FacingCell * offset;
					Thing t = ThingMaker.MakeThing(def, stuff);
					t.SetFaction(faction);
					GenSpawn.Spawn(t, pos, map);
				}
			}
		}
	}
}

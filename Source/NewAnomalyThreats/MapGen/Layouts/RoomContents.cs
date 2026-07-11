using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace NAT
{
	public class RoomContents_VaultRoom : RoomContentsWorker
	{
		public override void PostFillRooms(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
		{
			base.PostFillRooms(map, room, faction, threatPoints);
			foreach (CellRect rect in room.rects)
			{
				foreach (IntVec3 cell in rect.EdgeCells)
				{
					Thing t = cell.GetDoor(map);
					if (t != null && t.def != NATDefOf.NAT_ContainmentDoor)
					{
						t.Destroy();
						Thing door = ThingMaker.MakeThing(NATDefOf.NAT_ContainmentDoor);
						door.SetFaction(faction);
						GenSpawn.Spawn(door, cell, map);
					}
				}
			}
		}
	}

	public class RoomContents_ContainmentRoom : RoomContents_VaultRoom
	{
		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
		{
			base.FillRoom(map, room, faction, threatPoints);
		}

		public override void PostFillRooms(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
		{
			base.PostFillRooms(map, room, faction, threatPoints);
			List<Thing> list = new List<Thing>();
			foreach(CellRect rect in room.rects)
			{
				foreach (IntVec3 cell in rect.Cells)
				{
					Thing t = cell.GetFirstThing(map, NATDefOf.NAT_AncientHoldingPlatform);
					if (t != null && !list.Contains(t))
					{
						list.Add(t);
					}
				}
			}
			List<Pair<IntVec3, Rot4>> cells = new List<Pair<IntVec3, Rot4>>();
			foreach (Thing t in list)
			{
				IntVec3 pos = t.Position;
				foreach(Rot4 rot in Rot4.AllRotations)
				{
					IntVec3 cell1 = pos + new IntVec3(0,0,3).RotatedBy(rot);
					for (int i = -1; i < 2; i++)
					{
						IntVec3 cell2 = cell1 + new IntVec3(i, 0, 0).RotatedBy(rot);
						if(Validator(cell2, rot))
						{
							cells.Add(new Pair<IntVec3, Rot4>(cell2, rot));
						}
					}
				}
			}
			int count = Mathf.Max(Mathf.Min(cells.Count, 3), GenMath.RoundRandom((float)cells.Count * 0.6f));
			if (count > 3)
			{
				cells.Shuffle();
			}
			for (int i = 0; i < count; i++)
			{
				Thing t = ThingMaker.MakeThing(NATDefOf.NAT_AncientElectricInhibitor);
				t.SetFaction(faction);
				GenSpawn.Spawn(t, cells[i].First, map, cells[i].Second);
			}
			bool Validator(IntVec3 c, Rot4 r)
			{
				if (!NATDefOf.NAT_AncientElectricInhibitor.CanSpawnAt(c, r, map))
				{
					return false;
				}
				if((c + new IntVec3(0, 0, 2).RotatedBy(r)).GetDoor(map) != null)
				{
					return false;
				}
				if(cells.Any(x => x.Second != r && x.First.DistanceTo(c) <= 2))
				{
					return false;
				}
				foreach (IntVec3 item in GenAdj.OccupiedRect(c, r, NATDefOf.NAT_AncientElectricInhibitor.Size))
				{
					if (!item.InBounds(map))
					{
						return false;
					}
					if (!c.Walkable(map))
					{
						return false;
					}
					if (map.edificeGrid[item] != null)
					{
						return false;
					}
					if(item.GetDoor(map) != null)
					{
						return false;
					}
					foreach (Thing thing in c.GetThingList(map))
					{
						if (GenSpawn.SpawningWipes(NATDefOf.NAT_AncientElectricInhibitor, thing.def, ignoreDestroyable: true) && !thing.def.destroyable)
						{
							return false;
						}
					}
				}
				if (!GenConstruct.CanBuildOnTerrain(NATDefOf.NAT_AncientElectricInhibitor, c, map, r))
				{
					return false;
				}
				if (!GenConstruct.NotBlockingAnyInteractionCells(NATDefOf.NAT_AncientElectricInhibitor, c, r, map))
				{
					return false;
				}
				return true;
			}
		}
	}

	public class RoomContents_VaultStorageRoom : RoomContents_VaultRoom
	{
		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
		{
			RoomGenUtility.GenerateRows(ThingDefOf.Shelf, room, map, ThingDefOf.Steel);
			ThingSetMakerDef thingSetMakerDef = room.defs.FirstOrDefault((LayoutRoomDef x) => x.thingSetMakerDef != null)?.thingSetMakerDef;
			if (thingSetMakerDef != null)
			{
				ThingSetMakerParams parms = new ThingSetMakerParams
				{
					totalMarketValueRange = new FloatRange(2200f)
				};
				List<Thing> items = thingSetMakerDef.root.Generate(parms);
				GenerateItems(map, room, items);
			}
			base.FillRoom(map, room, faction, threatPoints);
		}

		private void GenerateItems(Map map, LayoutRoom room, List<Thing> items)
		{
			int num = 999;
			while (items.Count > 0 && num-- > 0)
			{
				Thing itemToSpawn = items.Last();
				items.Remove(itemToSpawn);
				if (room.TryGetRandomCellInRoom(map, out var cell, 0, 0, (IntVec3 c) => ShelfValidator(map, c, itemToSpawn.def), ignoreBuildings: true))
				{
					GenSpawn.Spawn(itemToSpawn, cell, map).SetForbidden(value: true);
					continue;
				}
				break;
			}
		}

		private bool ShelfValidator(Map map, IntVec3 c, ThingDef itemDef)
		{
			if (!(c.GetFirstThing(map, ThingDefOf.Shelf) is Building_Storage building_Storage))
			{
				return false;
			}
			if (building_Storage.SpaceRemainingFor(itemDef) == 0)
			{
				return false;
			}
			return true;
		}
	}

	/*public class RoomContents_ResearchCenterRoom : RoomContents_VaultRoom
	{
		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
		{
			List<IntVec3> cells = GenerateRoomLoot(map, room, faction);
			base.FillRoom(map, room, faction, threatPoints);
		}

		private List<IntVec3> GenerateRoomLoot(Map map, LayoutRoom room, Faction faction)
		{
			float value = Rand.Value;
			if (value < 0)
			{
				return GenerateAltar(room.rects[0].CenterCell, map).OccupiedRect().ExpandedBy(1).Cells.ToList();
			}
		}

		private Thing GenerateAltar(IntVec3 cell, Map map)
		{

		}
	}*/

	public class RoomContents_FacilityCorridor : RoomContents_Corridor
	{
		private static readonly FloatRange ShelvesPer10EdgeCells = new FloatRange(0.35f, 0.35f);

		private static readonly IntRange ShelfGroupSizeRange = new IntRange(1, 2);

		protected override ThingDef DoorThing => NATDefOf.NAT_ContainmentDoor;

		protected override IntRange ExteriorDoorCount => new IntRange(2, 3);

		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
		{
			base.FillRoom(map, room, faction, threatPoints);
			SpawnShelves(map, room);
		}

		private static void SpawnShelves(Map map, LayoutRoom room)
		{
			float num = (float)room.rects.Sum((CellRect r) => r.ContractedBy(1).EdgeCellsCount) / 10f;
			int count = Mathf.Max(Mathf.RoundToInt(ShelvesPer10EdgeCells.RandomInRange * num), 1);
			RoomGenUtility.FillAroundEdges(ThingDefOf.Shelf, count, ShelfGroupSizeRange, room, map, null, null, 1, 0, ThingDefOf.Steel);
		}
	}
}

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
	public class LayoutWorker_AncientResearchFacility : LayoutWorker_AncientRuins
	{
		protected override float RoomToExteriorDoorRatio => 0f;

		protected override bool SpawnRandomBlastDoors => false;

		public LayoutWorker_AncientResearchFacility(LayoutDef def)
			: base(def)
		{
		}

		protected override StructureLayout GetStructureLayout(StructureGenParams parms, CellRect rect)
		{
			LayoutStructureSketch sketch = parms.sketch;
			int minRoomHeight = base.Def.minRoomHeight;
			return RoomLayoutGenerator.GenerateRandomLayout(minRoomWidth: base.Def.minRoomWidth, minRoomHeight: minRoomHeight, areaPrunePercent: 0.1f, canRemoveRooms: true, generateDoors: false, maxMergeRoomsRange: new IntRange(2, 4), sketch: sketch, container: rect, corridor: base.Def.corridorDef, corridorExpansion: 2, corridorShapes: base.Def.corridorShapes, canDisconnectRooms: false);
		}

		public override void Spawn(LayoutStructureSketch layoutStructureSketch, Map map, IntVec3 pos, float? threatPoints = null, List<Thing> allSpawnedThings = null, bool roofs = true, bool canReuseSketch = false, Faction faction = null)
		{
			IntVec3 offset = ((!layoutStructureSketch.spawned) ? pos : (pos - layoutStructureSketch.center));
			foreach (LayoutRoom r in layoutStructureSketch.structureLayout.Rooms)
			{
				foreach (IntVec3 c in r.Cells)
				{
					IntVec3 cell = c + offset;
					map.terrainGrid.SetTerrain(cell, TerrainDefOf.AncientConcrete);
				}
			}
			base.Spawn(layoutStructureSketch, map, pos, threatPoints, allSpawnedThings, roofs, canReuseSketch, faction);
		}

		protected override void PostLayoutFlushedToSketch(LayoutStructureSketch parms)
		{
			base.PostLayoutFlushedToSketch(parms);
		}

		protected override void ReplaceRandomBlastDoors(LayoutSketch sketch)
		{
			int num = Mathf.CeilToInt((float)sketch.Things.Count((SketchThing thing) => thing.def.IsDoor) * RandomBlastDoorRatio);
			foreach (SketchThing item in sketch.Things.InRandomOrder())
			{
				if (item.def.IsDoor && item.def != NATDefOf.NAT_ContainmentDoor)
				{
					item.def = NATDefOf.NAT_ContainmentDoor;
					item.stuff = null;
					num--;
					if (num <= 0)
					{
						break;
					}
				}
			}
		}
	}
}

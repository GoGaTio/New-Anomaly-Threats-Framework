using JetBrains.Annotations;
using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace NAT.MapGen
{
	public static class DungeonUtility
	{
		public static List<CellRect> GenerateRects(CellRect mainRect, int minSize)
		{
			List<CellRect> rects = new List<CellRect>() { mainRect };
			List<CellRect> outRects = new List<CellRect>();
			CellRect rect1 = new CellRect();
			CellRect rect2 = new CellRect();
			while (rects.TryRandomElement((x)=> CanSplit(x) , out CellRect rect))
			{
				if(TrySplitRect(rect, minSize, out rect1, out rect2))
				{
					rects.Remove(rect);
					if (CanSplit(rect1))
					{
						rects.Add(rect1);
					}
					else
					{
						outRects.Add(rect1);
					}
					if (CanSplit(rect2))
					{
						rects.Add(rect2);
					}
					else
					{
						outRects.Add(rect2);
					}
				}
			}
			return outRects;
			bool CanSplit(CellRect rect)
			{
				return rect.Width > minSize * 2 || rect.Height > minSize * 2;
			}
		}

		public static bool TrySplitRect(CellRect baseRect, int minSize, out CellRect rect1, out CellRect rect2)
		{
			bool canSplitHor = baseRect.Width > minSize * 2;
			bool canSplitVer = baseRect.Height > minSize * 2;
			if (!canSplitHor && !canSplitVer)
			{
				rect1 = new CellRect();
				rect2 = new CellRect();
				return false;
			}
			if(canSplitHor && (!canSplitVer || Rand.Bool))
			{
				int splitX = new IntRange(minSize, baseRect.Width - minSize).RandomInRange;
				rect1 = new CellRect(baseRect.minX, baseRect.minZ, splitX + 1, baseRect.Height);
				rect2 = new CellRect(baseRect.minX + splitX, baseRect.minZ, baseRect.Width - splitX, baseRect.Height);
			}
			else
			{
				int splitZ = new IntRange(minSize, baseRect.Height - minSize).RandomInRange;
				rect1 = new CellRect(baseRect.minX, baseRect.minZ, baseRect.Width, splitZ + 1);
				rect2 = new CellRect(baseRect.minX, baseRect.minZ + splitZ, baseRect.Width, baseRect.Height - splitZ);
			}
			return true;
		}

		[DebugAction("NAT", "Test area (rect)", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
		public static void TSET()
		{
			DebugToolsGeneral.GenericRectTool("TEST", delegate (CellRect rect)
			{
				Map map = Find.CurrentMap;
				GenDebug.ClearArea(rect, map);
				List<CellRect> list = GenerateRects(rect, 5);
				foreach(CellRect r in list)
				{
					TerrainDef def = DefDatabase<TerrainDef>.GetRandom();
					while(def.isFoundation)
					{
						def = DefDatabase<TerrainDef>.GetRandom();
					}
					foreach(IntVec3 cell in r.ContractedBy(1))
					{
						map.terrainGrid.SetTerrain(cell, def);
					}
				}
			});
		}
	}
}

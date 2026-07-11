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
using static HarmonyLib.Code;
using static RimWorld.MechClusterSketch;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace NAT
{
	public class Building_VaultDoor : Building_SupportedDoor
	{
		[Unsaved(false)]
		private Graphic valveGraphic;

		private Graphic ValveGraphic
		{
			get
			{
				if (valveGraphic == null)
				{
					valveGraphic = GraphicDatabase.Get<Graphic_Single>("Things/Building/NAT_ContainmentDoor/NAT_ContainmentDoor_Valve", ShaderDatabase.CutoutComplex, def.graphicData.drawSize, Color.white);
				}
				return valveGraphic;
			}
		}

		protected bool locked = true;

		public int ticksUnlocking = 0;

		public virtual int TicksToUnlock => 240;

		public bool Locked => locked;

		public float UnlockPct => (float)ticksUnlocking / (float)TicksToUnlock;

		protected override bool CheckFaction => false;

		protected override bool AlwaysOpen => !Locked;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
		}

		public override bool PawnCanOpen(Pawn p)
		{
			if (Locked)
			{
				return false;
			}
			return base.PawnCanOpen(p);
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			Rot4 rotation = base.Rotation;
			float angle = Quaternion.AngleAxis(UnlockPct * 180f, Vector3.up).eulerAngles.y;
			Vector3 vector = new Vector3(0f, 0f, -def.size.x);
			rotation.Rotate(RotationDirection.Clockwise);
			vector = rotation.AsQuat * vector;
			Vector3 vector2 = drawLoc;
			vector2.y = AltitudeLayer.Building.AltitudeFor();
			vector2 += vector * 0.45f * OpenPct;
			if (rotation == Rot4.West || rotation == Rot4.East)
			{
				ValveGraphic.Draw(vector2, rotation, this, angle);
			}
			else
			{
				vector2.x -= 0.3f;
				ValveGraphic.Draw(vector2, rotation, this, angle);
				vector2.x += 0.6f;
				angle = 360f - angle;
				ValveGraphic.Draw(vector2, rotation, this, angle);
			}
		}

		public void TickInteracting()
		{
			if (locked)
			{
				ticksUnlocking++;
				if (ticksUnlocking >= TicksToUnlock)
				{
					locked = false;
					base.DoorOpen();
				}
			}
			else
			{
				ticksUnlocking--;
				if (ticksUnlocking <= 0)
				{
					locked = true;
					if (!DoorTryClose())
					{
						foreach (IntVec3 item in this.OccupiedRect())
						{
							List<Thing> thingList = item.GetThingList(base.Map);
							for (int i = 0; i < thingList.Count; i++)
							{
								Thing thing = thingList[i];
								if (thing.def.category == ThingCategory.Item || thing is Corpse)
								{
									thing.DeSpawn();
									GenPlace.TryPlaceThing(thing, item, Map, ThingPlaceMode.Near, extraValidator: x => !this.OccupiedRect().Contains(x));
								}
								else if(thing is Pawn pawn)
								{
									for (int j = 0; j < 4; j++)
									{
										if((item + GenAdj.CardinalDirections[j]).Walkable(Map))
										{
											pawn.Position = item + GenAdj.CardinalDirections[j];
											pawn.filth.Notify_EnteredNewCell();
										}
									}
								}
							}
						}
						DoorTryClose();
					}
				}
			}
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
			{
				yield return floatMenuOption;
			}
			if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
			{
				yield return new FloatMenuOption((Locked ? "CannotOpen".Translate(this) : "NAT_CannotClose".Translate(this)) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
				yield break;
			}
			if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				yield return new FloatMenuOption((Locked ? "CannotOpen".Translate(this) : "NAT_CannotClose".Translate(this)) + ": " + "Incapable".Translate().CapitalizeFirst(), null);
				yield break;
			}
			yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(Locked ? "Open".Translate(this) : "NAT_Close".Translate(this), delegate
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(NATDefOf.NAT_InteractWithVault, this), JobTag.Misc);
			}), selPawn, this);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (!DebugSettings.ShowDevGizmos)
			{
				yield break;
			}
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "DEV: Change state";
			command_Action.action = delegate
			{
				locked = !locked;
			};
			yield return command_Action;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref locked, "locked");
			Scribe_Values.Look(ref ticksUnlocking, "ticksUnlocking");
		}
	}
}

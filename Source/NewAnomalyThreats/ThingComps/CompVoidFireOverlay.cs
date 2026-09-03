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
	public class CompProperties_VoidFireOverlay : CompProperties_FireOverlay
	{
		public CompProperties_VoidFireOverlay()
		{
			compClass = typeof(CompVoidFireOverlay);
		}

		public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
		{
			GhostUtility.GhostGraphicFor(CompVoidFireOverlay.DarklightGraphic, thingDef, ghostCol).DrawFromDef(center.ToVector3ShiftedWithAltitude(drawAltitude), rot, thingDef);
		}
	}

	[StaticConstructorOnStartup]
	public class CompVoidFireOverlay : CompFireOverlayBase
	{
		protected CompRefuelable refuelableComp;

		public static readonly Graphic DarklightGraphic = GraphicDatabase.Get<Graphic_Flicker>("Things/Mote/NAT_VoidFire", ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);

		public new CompProperties_VoidFireOverlay Props => (CompProperties_VoidFireOverlay)props;

		public override void PostDraw()
		{
			base.PostDraw();
			if (refuelableComp == null || refuelableComp.HasFuel)
			{
				Vector3 drawPos = parent.DrawPos;
				drawPos.y += 0.03658537f;
				DarklightGraphic.Draw(drawPos, Rot4.North, parent);
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			refuelableComp = parent.GetComp<CompRefuelable>();
		}
	}
}

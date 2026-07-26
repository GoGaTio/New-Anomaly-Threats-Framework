using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace NAT
{
	public class CompProperties_ConstantFleckEmitter : CompProperties
	{
		public FleckDef fleck;

		public SimpleCurve sizeCurve;

		public int countEmitPerTick = 1;

		public int startEmitFromTick = 0;

		public SoundDef soundOnEmitStart;

		public CompProperties_ConstantFleckEmitter()
		{
			this.compClass = typeof(CompConstantFleckEmitter);
		}
	}
	
	public class CompConstantFleckEmitter : ThingComp
    {
        public CompProperties_ConstantFleckEmitter Props => (CompProperties_ConstantFleckEmitter)this.props;

		public static FieldInfo originField = AccessTools.Field(typeof(Projectile), "origin");

		public static FieldInfo destinationField = AccessTools.Field(typeof(Projectile), "destination");

		private int lifeTime = 0;

		private bool isProjectile = true;

		protected Vector3? origin = null;

		protected Vector3? destination = null;

		public Vector3 DrawPos
		{
			get
			{
				Vector3 pos = parent.DrawPos;
				if (isProjectile)
				{
					float num = parent.def.projectile.arcHeightFactor;
					if (parent is Projectile proj && num > 0)
					{
						if(origin == null)
						{
							origin = (Vector3)originField.GetValue(proj);
						}
						if (destination == null)
						{
							destination = (Vector3)destinationField.GetValue(proj);
						}
						float num2 = (destination - origin).Value.MagnitudeHorizontalSquared();
						if (num * num > num2 * 0.2f * 0.2f)
						{
							num = Mathf.Sqrt(num2) * 0.2f;
						}
						float num3 = (origin - destination).Value.magnitude / parent.def.projectile.SpeedTilesPerTick;
						if (num3 <= 0f)
						{
							num3 = 0.001f;
						}
						pos += new Vector3(0f, 0f, 1f) * num * GenMath.InverseParabola(Mathf.Clamp01((float)(lifeTime) / num3));
					}
					else
					{
						isProjectile = false;
					}
				}
				return pos;
			}
		}

		public override void CompTick()
        {
			if (Props.startEmitFromTick <= lifeTime && parent.Spawned && parent.Position.ShouldSpawnMotesAt(parent.MapHeld))
			{
				if(Props.soundOnEmitStart != null && Props.startEmitFromTick == lifeTime)
				{
					Props.soundOnEmitStart.PlayOneShot(parent);
				}
				ThrowFleck(DrawPos, lifeTime);
			}
			lifeTime++;
		}

        public void ThrowFleck(Vector3 drawPos, float evaluate)
        {
            for(int i = 0; i < Props.countEmitPerTick; i++)
            {
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, parent.MapHeld, Props.fleck);
				dataStatic.scale = Props.sizeCurve.Evaluate(evaluate);
				dataStatic.rotationRate = Rand.Range(-30, 30);
				dataStatic.velocityAngle = Rand.Range(-180, 180);
				dataStatic.velocitySpeed = Mathf.Clamp01(1 - dataStatic.scale);
				parent.MapHeld.flecks.CreateFleck(dataStatic);
			}
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lifeTime, "NAT_lifeTime");
		}
    }
}
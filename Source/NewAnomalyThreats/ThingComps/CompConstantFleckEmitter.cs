using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using System.Reflection;
using System;
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

        private int lifeTime = 0;

        public override void CompTick()
        {
			if (Props.startEmitFromTick <= lifeTime && parent.Spawned && parent.Position.ShouldSpawnMotesAt(parent.MapHeld))
			{
				if(Props.soundOnEmitStart != null && Props.startEmitFromTick == lifeTime)
				{
					Props.soundOnEmitStart.PlayOneShot(parent);
				}
				ThrowFleck(parent.DrawPos, lifeTime);
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
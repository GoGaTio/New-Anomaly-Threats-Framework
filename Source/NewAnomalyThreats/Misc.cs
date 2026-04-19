using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace NAT
{
	public interface IAffectedByDisruptor
	{
		void Affect();
	}
	public class IncidentExtension : DefModExtension
	{
		public ThingDef thingDef;

		public ThingDef skyfallerDef;

		public PawnKindDef pawnKindDef;

		public List<ThingDef> thingDefList = new List<ThingDef>();

		public FactionDef factionDef;
	}

	public class DamageExtension : DefModExtension
	{
		public DamageDef damageDef;

		public int amount;

		public float armorPenetration;
	}

	public class DamageWorker_Deadlife : DamageWorker
	{
		public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
		{
			if (c.DistanceTo(explosion.Position) < explosion.radius / 2f && canThrowMotes)
			{
				GasUtility.AddDeadifeGas(c, explosion.Map, explosion.instigator?.Faction ?? Faction.OfEntities, 255);
			}
			else
			{
				GasUtility.MarkDeadlifeCorpsesForFaction(c, explosion.Map, explosion.instigator?.Faction ?? Faction.OfEntities, 255);
			}
			base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
		}
	}
}

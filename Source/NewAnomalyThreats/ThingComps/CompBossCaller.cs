using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NAT
{
	public class CompProperties_BossCaller : CompProperties
	{
		public AnomalyBossDef bossDef;

		public CompProperties_BossCaller()
		{
			compClass = typeof(CompBossCaller);
		}
	}

	public class CompBossCaller : ThingComp
	{
		public CompProperties_BossCaller Props => (CompProperties_BossCaller)props;


	}
}

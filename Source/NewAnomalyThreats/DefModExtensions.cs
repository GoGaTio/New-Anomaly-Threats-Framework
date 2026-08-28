using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace NAT
{
	public class CanHaveFactionExtension : DefModExtension
	{
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
}

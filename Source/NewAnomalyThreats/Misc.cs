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

namespace NAT
{
	public class Graphic_AltarMask : Graphic_WithPropertyBlock
	{
		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			CompTradeAltar comp = thing.TryGetComp<CompTradeAltar>();
			if (comp == null)
			{
				return;
			}
			Color value = colorTwo;
			value.a = comp.Alpha;
			propertyBlock.SetColor(ShaderPropertyIDs.ColorTwo, value);
			base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}
	}

	public interface IAlwaysTargetable
	{
	}

	public class CanHaveFactionExtension : DefModExtension
	{
	}

	public class Alert_AnomalyBoss : Alert_Critical
	{
		public override string GetLabel()
		{
			string s = "";
			foreach(AnomalyBossDef def in NewAnomalyThreatsUtility.Comp.bossManager.GetIncomingBosses())
			{
				if (!s.NullOrEmpty())
				{
					s += "\n";
				}
				s += "NAT_BossIncomingLabel".Translate(def.LabelCap);
			}
			return s;
		}

		public override TaggedString GetExplanation()
		{
			return "NAT_BossIncomingDesc".Translate();
		}

		public override AlertReport GetReport()
		{
			return NewAnomalyThreatsUtility.Comp.bossManager.AnyBossIncoming;
		}
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

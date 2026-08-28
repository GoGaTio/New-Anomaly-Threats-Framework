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
	public class CapacityImpactorBoss : PawnCapacityUtility.CapacityImpactor
	{
		public override string Readable(Pawn pawn)
		{
			return "NAT_IsBoss".Translate();
		}
	}

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

	public class Graphic_EyedMachine : Graphic_WithPropertyBlock
	{
		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			CompEyedMachine comp = thing.TryGetComp<CompEyedMachine>();
			if (comp == null)
			{
				return;
			}
			Color value = colorTwo;
			value.a = Mathf.Clamp01(comp.ChargePercent);
			propertyBlock.SetColor(ShaderPropertyIDs.ColorTwo, value);
			base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}
	}

	public class Alert_AnomalyBoss : Alert_Critical
	{
		public override string GetLabel()
		{
			string s = "";
			int bossCount = 0;
			foreach(AnomalyBossDef def in NewAnomalyThreatsUtility.Comp.bossManager.GetIncomingBosses())
			{
				if (!s.NullOrEmpty())
				{
					s += ", ";
				}
				bossCount++;
				s += def.LabelCap;
			}
			return bossCount > 1 ? "NAT_MultipleBossesIncomingLabel".Translate(s) : "NAT_BossIncomingLabel".Translate(s);
		}

		public override TaggedString GetExplanation()
		{
			string s = "";
			int bossCount = 0;
			foreach (AnomalyBossDef def in NewAnomalyThreatsUtility.Comp.bossManager.GetIncomingBosses())
			{
				bossCount++;
				s += "\n  " + def.LabelCap;
			}
			return bossCount > 1 ? "NAT_MultipleBossesIncomingDesc".Translate() : "NAT_BossIncomingDesc".Translate() + ":" + s;
		}

		public override AlertReport GetReport()
		{
			return NewAnomalyThreatsUtility.Comp.bossManager.AnyBossIncoming;
		}
	}

	public class Rule_TranslateByKey : Rule
	{
		public string key;

		public override float BaseSelectionWeight => 1f;

		public override Rule DeepCopy()
		{
			Rule_TranslateByKey obj = (Rule_TranslateByKey)base.DeepCopy();
			obj.key = key;
			return obj;
		}

		public override string Generate()
		{
			return key.Translate();
		}

		public override string ToString()
		{
			return keyword + "->(" + key + ")";
		}
	}
}

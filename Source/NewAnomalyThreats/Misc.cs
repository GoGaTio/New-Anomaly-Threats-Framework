using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace NAT
{
	public class ThingDefParmsClass : IExposable
	{
		public ThingDef thingDef;

		public float value;

		public float reserveValue;

		public int count = 1;

		public IntRange intRange;

		public FloatRange floatRange;

		public ThingDef stuff;

		public QualityCategory quality = QualityCategory.Normal;

		public ThingDefParmsClass()
		{
		}

		public ThingDefParmsClass(ThingDef thingDef, int count)
		{
			if (count < 0)
			{
				Log.Warning("Tried to set ThingDefCountClass count to " + count + ". thingDef=" + thingDef);
				count = 0;
			}
			this.thingDef = thingDef;
			this.count = count;
		}

		public void ExposeData()
		{
			Scribe_Defs.Look(ref thingDef, "thingDef");
			Scribe_Values.Look(ref value, "value");
			Scribe_Values.Look(ref reserveValue, "reserveValue");
			Scribe_Values.Look(ref count, "count", 1);
			Scribe_Values.Look(ref intRange, "intRange");
			Scribe_Values.Look(ref floatRange, "floatRange");
			Scribe_Defs.Look(ref stuff, "stuff");
			Scribe_Values.Look(ref quality, "quality", QualityCategory.Normal);
		}

		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			XmlHelper.ParseElements(this, xmlRoot, "thingDef", "value");
		}

		public override string ToString()
		{
			return string.Format("({0}x {1})", value, (thingDef != null) ? thingDef.defName : "null");
		}

		public override int GetHashCode()
		{
			return thingDef.shortHash + count << 16;
		}
	}

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

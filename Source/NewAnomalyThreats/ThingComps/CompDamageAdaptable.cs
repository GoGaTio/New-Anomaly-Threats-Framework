using DelaunatorSharp;
using Gilzoide.ManagedJobs;
using HarmonyLib;
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
using System.Security.Cryptography;
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

namespace NAT
{
	public class CompProperties_DamageAdaptable : CompProperties
	{
		public List<DamageDef> ignoreDamageDefs = new List<DamageDef>();

		public CompProperties_DamageAdaptable()
		{
			compClass = typeof(CompDamageAdaptable);
		}
	}

	public class CompDamageAdaptable : ThingComp
	{
		public CompProperties_DamageAdaptable Props => (CompProperties_DamageAdaptable)props;

		public Dictionary<DamageDef, float> adaptedDamages = new Dictionary<DamageDef, float>();

		public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
			float damage = dinfo.Amount;
			float adaptation;
			if (adaptedDamages.TryGetValue(dinfo.Def, out adaptation))
			{
				dinfo.SetAmount(damage * adaptation);
			}
			else
			{
				adaptation = 0f;
			}
			adaptedDamages.SetOrAdd(dinfo.Def, adaptation);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref adaptedDamages, "adaptedDamages", LookMode.Def, LookMode.Value);
		}
	}
}
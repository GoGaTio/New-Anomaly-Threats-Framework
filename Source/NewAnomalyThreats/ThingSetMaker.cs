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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using DelaunatorSharp;
using Gilzoide.ManagedJobs;
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
	public class ThingSetMaker_Pawns : ThingSetMaker
	{
		public IntRange countRange;

		public FloatRange ageRange;

		public PawnKindDef kindDef;

		public ThingDef weaponOverride;

		protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
		{
			int num = countRange.RandomInRange;
			float age = ageRange.RandomInRange;
			PawnGenerationRequest req = new PawnGenerationRequest(kindDef, Faction.OfEntities, fixedBiologicalAge: age, fixedChronologicalAge: age);
			for (int i = 0; i < num; i++)
			{
				Pawn rust = PawnGenerator.GeneratePawn(req);
				if(weaponOverride != null)
                {
					rust.equipment.DestroyAllEquipment();
					rust.equipment.AddEquipment(ThingMaker.MakeThing(weaponOverride) as ThingWithComps);
				}
				rust.inventory.DestroyAll();
				rust.apparel.DestroyAll();
				outThings.Add(rust);
			}
		}

		protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
		{
			yield return kindDef.race;
		}
	}
}

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
using HarmonyLib;

namespace NAT
{
	public class CompProperties_UniqueName : CompProperties
	{
		public RulePackDef nameMaker;

		public CompProperties_UniqueName()
		{
			compClass = typeof(CompUniqueName);
		}
	}
	public class CompUniqueName : ThingComp
	{
		public CompProperties_UniqueName Props => (CompProperties_UniqueName)props;

		protected TaggedString titleInt = null;

		public string Title
		{
			get
			{
				if (titleInt.NullOrEmpty())
				{
					return null;
				}
				return titleInt;
			}
			set
			{
				titleInt = value;
			}
		}

		public override void PostPostMake()
		{
			base.PostPostMake();
			titleInt = GenText.CapitalizeAsTitle(GrammarResolver.Resolve("r_name", new GrammarRequest
			{
				Includes = { Props.nameMaker }
			}));
		}

		public override string TransformLabel(string label)
		{
			return Title ?? base.TransformLabel(label);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref titleInt, "titleInt", null);
		}
	}
}
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
	public class ChainingBeam : Projectile_Explosive
	{
		public int chainingsLeft = -1;

		public bool isChained = false;

		//public override Vector3 ExactPosition => destination + Vector3.up * def.Altitude;

		public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
		{
			if(!isChained)
			{
				chainingsLeft = 10;
			}
			base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
			/*Vector3 offsetA = (ExactPosition - launcher.Position.ToVector3Shifted()).Yto0().normalized * def.projectile.beamStartOffset;
			if (def.projectile.beamMoteDef != null)
			{
				MoteMaker.MakeInteractionOverlay(def.projectile.beamMoteDef, isChained ? new TargetInfo(origin.ToIntVec3(), Map) : launcher, usedTarget.ToTargetInfo(base.Map), isChained ? Vector3.zero : offsetA, Vector3.zero);
			}
			Position = ExactPosition.ToIntVec3();
			ImpactSomething();*/
		}

		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			IntVec3 cell = Position;
			if (chainingsLeft > 0)
			{
				chainingsLeft--;
				CellRect rect = CellRect.FromCell(cell).ExpandedBy(6);
				if (rect.TryFindRandomCell(out var target, c => c != cell && c.InHorDistOf(cell, 6f) && IsHittable((c - cell).AngleFlat) && GenSight.LineOfSight(cell, c, Map, skipFirstCell: true)))
				{
					LaunchChained(target);
				}
			}
			base.Impact(hitThing, blockedByShield);
		}

		public static bool IsHittable(float angle)
		{
			while(angle > 180f)
			{
				angle -= 180f;
			}
			if(angle < 30f)
			{
				return false;
			}
			if(angle > 150f)
			{
				return false;
			}
			return true;
		} 

		public void LaunchChained(LocalTargetInfo target)
		{
			ChainingBeam projectile = (ChainingBeam)GenSpawn.Spawn(def, Position, Map);
			projectile.chainingsLeft = chainingsLeft;
			projectile.isChained = true;
			projectile.damageDefOverride = damageDefOverride;
			if(extraDamages.NullOrEmpty())
			{
				if (projectile.extraDamages == null)
				{
					projectile.extraDamages = new List<ExtraDamage>();
				}
				projectile.extraDamages.AddRange(extraDamages);
			}
			projectile.Launch(launcher, ExactPosition, target, target, this.HitFlags, preventFriendlyFire, equipment);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref chainingsLeft, "chainingsLeft");
		}
	}
}

using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NAT
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PostDefLoadedNotify : Attribute
	{
		
	}

	[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
	public class Patch_PreDefLoaded
	{
		[HarmonyPostfix]
		public static void Postfix(bool hotReload)
		{
			foreach (DefGeneratorDef generatorDef in DefDatabase<DefGeneratorDef>.AllDefs)
			{
				generatorDef.generator.AddImpliedDefs(hotReload);
			}
		}
	}

	[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PostResolve))]
	public class Patch_PostDefLoaded
	{
		[HarmonyPostfix]
		public static void Postfix()
		{
			try
			{
				Parallel.ForEach(GenTypes.AllTypesWithAttribute<PostDefLoadedNotify>(), Notify);
			}
			catch (Exception ex)
			{
				Log.Error("Could not permanently disable dev mode: " + ex);
			}
		}

		private static void Notify(Type type)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name == "Notify_DefsLoaded")
				{
					methodInfo.Invoke(null, null);
				}
			}
		}
	}

	[PostDefLoadedNotify]
	public static class AnomaliesCountAdjuster
	{
		public static void Notify_DefsLoaded()
		{
			foreach (EntityCodexEntryDef def in DefDatabase<EntityCodexEntryDef>.AllDefs)
			{
				if (def.HasModExtension<CodexEntryExtension>())
				{
					if(def.category.defName == "Basic")
					{
						MonolithLevelDefOf.Waking.entityCountCompletionRequired++;
					}
					else if (def.category.defName == "Advanced")
					{
						MonolithLevelDefOf.VoidAwakened.entityCountCompletionRequired++;
					}
				}
			}
		}
	}

	[PostDefLoadedNotify]
	public static class PathwayGiversTracker
	{
		public static void Notify_DefsLoaded()
		{
			string s = "";
			foreach (PawnKindDef kindDef in DefDatabase<PawnKindDef>.AllDefs)
			{
				if (!kindDef.rangedAttackInfectionPathways.NullOrEmpty() || !kindDef.meleeAttackInfectionPathways.NullOrEmpty())
				{
					s += "\n\n" + kindDef.label + " (" + kindDef.defName + ")";
					if (!kindDef.rangedAttackInfectionPathways.NullOrEmpty())
					{
						s += "\n" + "Ranged:";
						foreach (InfectionPathwayDef path in kindDef.rangedAttackInfectionPathways)
						{
							s += "\n" + path.label + " (" + path.defName + ")";
						}
					}
					if (!kindDef.meleeAttackInfectionPathways.NullOrEmpty())
					{
						s += "\n" + "Melee:";
						foreach (InfectionPathwayDef path in kindDef.meleeAttackInfectionPathways)
						{
							s += "\n" + path.label + " (" + path.defName + ")";
						}
					}
				}
			}
			foreach (HediffDef hediffDef in DefDatabase<HediffDef>.AllDefs)
			{
				if (!hediffDef.givesInfectionPathways.NullOrEmpty())
				{
					s += "\n\n" + hediffDef.label + " (" + hediffDef.defName + "):";
					foreach(InfectionPathwayDef path in hediffDef.givesInfectionPathways)
					{
						s += "\n" + path.label + " (" + path.defName + ")";
					}
				}
			}
			Log.Message(s);
		}
	}

	public class CodexEntryExtension : DefModExtension
	{
	}
}

using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;
using static HarmonyLib.Code;
using static RimWorld.Dialog_StylingStation;
using static System.Net.Mime.MediaTypeNames;

namespace NAT
{
	public abstract class SubModSettings
	{
		public SubModSettings()
		{
		}

		public virtual void DoSettings(Rect inRect)
		{
		}

		public abstract string SettingsName {  get; }

		public virtual void ExposeData()
		{
		}

		public string FileName
		{
			get
			{
				string s = SettingsName;
				for (int i = s.Length - 1; i >= 0; i--)
				{
					char c = s[i];
					if (c == ' ' || c == '.' || c == '\'' || c == '"' || c == '*')
					{
						s = s.Remove(i, 1);
					}
				}
				return s + ".xml";
			}
		}
	}

	public class AnomalyEvent : IExposable
	{
		public string defName;

		public float commonalityFactor = 1f;

		public float pointsFactor = 1f;

		public void ExposeData()
		{
			Scribe_Values.Look(ref defName, "defName");
			Scribe_Values.Look(ref commonalityFactor, "commonalityFactor");
			Scribe_Values.Look(ref pointsFactor, "pointsFactor");
		}

		public void Apply()
		{
			if (defName.NullOrEmpty())
			{
				return;
			}
			IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
			if (def?.Worker is IAnomalyEvent e)
			{
				if (e.AdjustPoints)
				{
					e.PointsFactor = pointsFactor;
				}
				e.CommonalityFactor = commonalityFactor;
			}
		}
	}

	public class NewAnomalyThreatsSettings : ModSettings
    {
		public float endingExtensionsChance = 1f;

		public bool allowDiscoveredEntitiesIncrease = true;

		public List<AnomalyEvent> events = new List<AnomalyEvent>();

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref events, "events", LookMode.Deep);
			Scribe_Values.Look(ref allowDiscoveredEntitiesIncrease, "allowDiscoveredEntitiesIncrease", defaultValue: true);
			Scribe_Values.Look(ref endingExtensionsChance, "endingExtensionsChance", defaultValue: 1f);
			base.ExposeData();
		}
	}

	public class NewAnomalyThreatsMod : Mod
	{
		public static string SubModsFolderPath => Path.Combine(GenFilePaths.ConfigFolderPath, "NewAnomalyThreats");

		public static Harmony harmony;

		public enum SettingsTab
		{
			General,
			Incidents,
			ModSpecific
		}

		private SettingsTab curTab;

		private Vector2 generalScrollPosition;

		private Vector2 incidentsScrollPosition;

		private Vector2 modSpecificScrollPosition;

		private List<TabRecord> tabs = new List<TabRecord>();

		public NewAnomalyThreatsSettings settings;

		public NewAnomalyThreatsMod(ModContentPack content) : base(content)
		{
			this.settings = GetSettings<NewAnomalyThreatsSettings>();
			try
			{
				CreateModClasses();
			}
			catch (Exception ex)
			{
				Log.Error("Exception while loading mod settings of New Anomaly Threats Framework: " + ex);
			}
			try
			{
				harmony = new Harmony("GoGaTio.NewAnomalyThreats.HarmonyPatch");
				harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());

				harmony.Patch((MethodBase)AccessTools.Method(typeof(MainTabWindow_Research), "UpdateSelectedProject", (Type[])null, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod(typeof(Patches_Research), "UniversalTranspiler", (Type[])null), (HarmonyMethod)null);
				harmony.Patch((MethodBase)AccessTools.Method(typeof(MainTabWindow_Research), "DrawProjectInfo", (Type[])null, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod(typeof(Patches_Research), "UniversalTranspiler", (Type[])null), (HarmonyMethod)null);
				harmony.Patch((MethodBase)AccessTools.Method(typeof(MainTabWindow_Research), "DrawStartButton", (Type[])null, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod(typeof(Patches_Research), "UniversalTranspiler", (Type[])null), (HarmonyMethod)null);
				harmony.Patch((MethodBase)AccessTools.Method(typeof(MainTabWindow_Research), "DrawRightRect", (Type[])null, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, new HarmonyMethod(typeof(Patches_Research), "UniversalTranspiler", (Type[])null), (HarmonyMethod)null);
			}
			catch (Exception ex)
			{
				Log.Error("Exception while initiating Harmony patches of New Anomaly Threats Framework: " + ex);
			}
		}

		public List<IAnomalyEvent> events = null;

		public List<SubModSettings> subMods = null;

		private SubModSettings selectedSubMod;

		public void LoadSubModSettings(SubModSettings subMod)
		{
			string path = Path.Combine(SubModsFolderPath, subMod.FileName);
			FileInfo fileInfo = new FileInfo(path);
			if (fileInfo.Exists)
			{
				try
				{
					Scribe.loader.InitLoading(path);
					try
					{
						subMod.ExposeData();
						Scribe.loader.FinalizeLoading();
					}
					catch
					{
						Scribe.ForceStop();
						throw;
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Exception loading {subMod.GetType()} from {path}: " + ex.ToString());
					Scribe.ForceStop();
				}
			}
		}

		public void CreateModClasses()
		{
			subMods = new List<SubModSettings>();
			foreach (Type type in typeof(SubModSettings).InstantiableDescendantsAndSelf())
			{
				try
				{
					SubModSettings subMod = (SubModSettings)Activator.CreateInstance(type);
					LoadSubModSettings(subMod);
					subMods.Add(subMod);
				}
				catch (Exception ex)
				{
					Log.Error("Error while instantiating a submod of type " + type?.ToString() + ": " + ex);
				}
			}
		}

		public override void WriteSettings()
		{
			if(settings.events == null)
			{
				settings.events = new List<AnomalyEvent>();
			}
			foreach(IAnomalyEvent item in events)
			{
				if (item.Def?.defName.NullOrEmpty() != false)
				{
					continue;
				}
				float num1 = item.CommonalityFactor;
				float num2 = item.PointsFactor;
				AnomalyEvent obj = new AnomalyEvent();
				obj.defName = item.Def.defName;
				obj.commonalityFactor = num1;
				obj.pointsFactor = num2;
				settings.events.RemoveAll(x => x.defName == obj.defName);
				settings.events.Add(obj);
				obj.Apply();
			}
			base.WriteSettings();
			foreach (SubModSettings subMod in subMods)
			{
				SaveSubModSettings(subMod);
			}
		}

		public void SaveSubModSettings(SubModSettings subMod)
		{
			if(subMod == null)
			{
				return;
			} 
			string path = Path.Combine(SubModsFolderPath, subMod.FileName);
			try
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(SubModsFolderPath);
				FileInfo fileInfo = new FileInfo(path);
				if (directoryInfo.Exists)
				{
					if (fileInfo.Exists)
					{
						fileInfo.Delete();
					}
				}
				else
				{
					directoryInfo.Create();
				}
				SafeSaver.Save(path, "settings", subMod.ExposeData);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception while saving {subMod.GetType()} to {path}: " + ex.ToString());
			}
		}

		public override string SettingsCategory()
		{
			return "New Anomaly Threats";
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			if(subMods == null)
			{
				CreateModClasses();
			}
			if(events == null)
			{
				events = new List<IAnomalyEvent>();
				foreach (IncidentDef def in DefDatabase<IncidentDef>.AllDefs)
				{
					if (def.Worker is IAnomalyEvent e)
					{
						events.Add(e);
					}
				}
			}
			Rect outRect = new Rect(inRect.x, inRect.y + 20, inRect.width, inRect.height - 20).ContractedBy(10);
			DrawTabs(outRect);
		}

		private void DrawTabs(Rect rect)
		{
			tabs.Clear();
			tabs.Add(new TabRecord("NAT_Settings_General".Translate().CapitalizeFirst(), delegate
			{
				curTab = SettingsTab.General;
			}, curTab == SettingsTab.General));
			if (!events.NullOrEmpty())
			{
				tabs.Add(new TabRecord("NAT_Settings_Incidents".Translate().CapitalizeFirst(), delegate
				{
					curTab = SettingsTab.Incidents;
				}, curTab == SettingsTab.Incidents));
			}
			if (!subMods.NullOrEmpty())
			{
				tabs.Add(new TabRecord("NAT_Settings_ModSpecific".Translate().CapitalizeFirst(), delegate
				{
					curTab = SettingsTab.ModSpecific;
				}, curTab == SettingsTab.ModSpecific));
			}
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(rect, tabs);
			rect = rect.ContractedBy(18f);
			switch (curTab)
			{
				case SettingsTab.General:
					DrawGeneralTab(rect);
					break;
				case SettingsTab.Incidents:
					DrawIncidentsTab(rect);
					break;
				case SettingsTab.ModSpecific:
					DrawModSpecificTab(rect);
					break;
			}
		}

		private void DrawGeneralTab(Rect rect)
		{
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(rect);
			bool flag1 = settings.allowDiscoveredEntitiesIncrease;
			listingStandard.CheckboxLabeled("NAT_Setting_AllowDiscoveredEntitiesIncrease".Translate(), ref flag1, "NAT_Setting_AllowDiscoveredEntitiesIncrease_Desc".Translate());
			if (flag1 != settings.allowDiscoveredEntitiesIncrease)
			{
				settings.allowDiscoveredEntitiesIncrease = flag1;
				foreach (EntityCodexEntryDef def in DefDatabase<EntityCodexEntryDef>.AllDefs)
				{
					if (def.HasModExtension<CodexEntryExtension>())
					{
						if (def.category.defName == "Basic")
						{
							MonolithLevelDefOf.Waking.entityCountCompletionRequired += flag1 ? 1 : -1;
						}
						else if (def.category.defName == "Advanced")
						{
							MonolithLevelDefOf.VoidAwakened.entityCountCompletionRequired += flag1 ? 1 : -1;
						}
					}
				}
			}
			settings.endingExtensionsChance = Mathf.RoundToInt(listingStandard.SliderLabeled("NAT_Setting_EndingExpansionsChance".Translate() + ": " + settings.endingExtensionsChance.ToStringPercent(), settings.endingExtensionsChance, 0f, 2f, tooltip: "NAT_Setting_EndingExpansionsChance_Desc".Translate()) * 20f) / 20f;
			listingStandard.End();
		}

		private float viewHeight;

		private void DrawIncidentsTab(Rect rect)
		{
			Rect viewRect = new Rect(0f, 0f, rect.width, viewHeight);
			if (viewRect.height >= rect.height)
			{
				viewRect.width -= 20f;
			}
			Widgets.BeginScrollView(rect, ref modSpecificScrollPosition, viewRect);
			float curY = 0;
			foreach (IAnomalyEvent item in events)
			{
				bool flag = item.AdjustPoints;
				Rect innerRect = new Rect(0f, curY, viewRect.width, flag ? 90f : 60f);
				if (Mouse.IsOver(innerRect))
				{
					Widgets.DrawHighlight(innerRect);
				}
				else
				{
					Widgets.DrawLightHighlight(innerRect);
				}
				using (new TextBlock(TextAnchor.MiddleCenter))
				{
					Widgets.Label(new Rect(0f, curY, viewRect.width, 30f), item.Def.LabelCap.Colorize(ColoredText.TipSectionTitleColor));
				}
				if (Widgets.ButtonText(new Rect(viewRect.width - 100f, curY, 100f, 30f).ContractedBy(3), "Reset".Translate()))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					item.CommonalityFactor = 1f;
					item.PointsFactor = 1f;
				}
				curY += 30f;
				item.CommonalityFactor = Widgets.HorizontalSlider(new Rect(0f, curY, viewRect.width, 30f), item.CommonalityFactor, 0f, 3f, label: "NAT_IncidentCommonalityFactor".Translate() + ": " + item.CommonalityFactor.ToStringPercent(), roundTo: 0.05f);
				curY += 30f;
				if (flag)
				{
					item.PointsFactor = Widgets.HorizontalSlider(new Rect(0f, curY, viewRect.width, 30f), item.PointsFactor, 0.1f, 2f, label: "NAT_IncidentPointsFactor".Translate() + ": " + item.PointsFactor.ToStringPercent(), roundTo: 0.05f);
					curY += 30f;
				}
				curY += 10f;
			}
			viewHeight = curY;
			Widgets.EndScrollView();
		}

		private void DrawModSpecificTab(Rect rect)
		{
			DoModListing(new Rect(rect.x, rect.y, 240f, rect.height));
			if(selectedSubMod != null)
			{
				selectedSubMod.DoSettings(new Rect(rect.x + 250f, rect.y, rect.width - 250f, rect.height));
			}
		}

		private void DoModListing(Rect rect)
		{
			Widgets.DrawMenuSection(rect);
			//Widgets.BeginGroup(rect);
			Rect viewRect = new Rect(0f, 0f, rect.width, (float)subMods.Count * 32f);
			if (viewRect.height >= rect.height)
			{
				viewRect.width -= 20f;
			}
			Widgets.BeginScrollView(rect, ref modSpecificScrollPosition, viewRect);
			float curY = 0;
			bool flag = false;
			foreach (SubModSettings subMod in subMods)
			{
				Rect innerRect = new Rect(0f, curY, viewRect.width, 32f);
				if (selectedSubMod == subMod)
				{
					Widgets.DrawHighlightSelected(innerRect);
				}
				else if (Mouse.IsOver(innerRect))
				{
					Widgets.DrawHighlight(innerRect);
				}
				else if (flag)
				{
					Widgets.DrawLightHighlight(innerRect);
				}
				using (new TextBlock(TextAnchor.MiddleLeft))
				{
					Widgets.Label(new Rect(innerRect.x + 5f, innerRect.y, innerRect.width - 5f, innerRect.height), subMod.SettingsName);
				}
				if (Widgets.ButtonInvisible(innerRect))
				{
					selectedSubMod = subMod;
				}
				flag = !flag;
				curY += 32f;
			}
			Widgets.EndScrollView();
			//Widgets.EndGroup();
		}
	}
}

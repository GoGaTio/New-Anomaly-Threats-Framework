using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Grammar;
using static System.Collections.Specialized.BitVector32;

namespace NAT
{
	public class QuestNode_Root_AncientResearchFacility : QuestNode
	{
		[MustTranslate]
		public string rewardLabel;

		[MustTranslate]
		public string rewardDesc;

		private static List<LandmarkDef> allowedLandmarksCached;

		private const string SitePartTag = "NAT_AncientResearchFacility";

		protected static List<LandmarkDef> AllowedLandmarks
		{
			get
			{
				if (ModsConfig.OdysseyActive && allowedLandmarksCached == null)
				{
					allowedLandmarksCached = new List<LandmarkDef>
					{
						LandmarkDefOf.Oasis,
						LandmarkDefOf.Lake,
						LandmarkDefOf.LakeWithIsland,
						LandmarkDefOf.LakeWithIslands,
						LandmarkDefOf.Pond,
						LandmarkDefOf.DryLake,
						LandmarkDefOf.ToxicLake,
						LandmarkDefOf.Wetland,
						LandmarkDefOf.HotSprings,
						LandmarkDefOf.CoastalIsland,
						LandmarkDefOf.Peninsula,
						LandmarkDefOf.Valley,
						LandmarkDefOf.Cavern,
						LandmarkDefOf.Chasm,
						LandmarkDefOf.Cliffs,
						LandmarkDefOf.Hollow,
						LandmarkDefOf.TerraformingScar,
						LandmarkDefOf.Dunes
					};
				}
				return allowedLandmarksCached;
			}
		}

		protected override bool TestRunInt(Slate slate)
		{
			if (!Find.Storyteller.difficulty.allowViolentQuests)
			{
				return false;
			}
			QuestGenUtility.TestRunAdjustPointsForDistantFight(slate);
			float num = slate.Get("points", 0f);
			if (num < 100f)
			{
				return false;
			}
			if (TryFindSiteTile(out var _, exitOnFirstTileFound: true))
			{
				return true;
			}
			return false;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			float num = slate.Get("points", 0f);
			if (num < 100f)
			{
				num = 100f;
			}
			TryFindSiteTile(out var tile);
			Log.Message(tile.Valid);
			Faction faction = Faction.OfAncientsHostile;
			slate.Set("faction", faction);
			IEnumerable<SitePartDef> source = DefDatabase<SitePartDef>.AllDefs.Where((SitePartDef def) => def.tags != null && def.tags.Contains(SitePartTag));
			SitePartDef sitePart = source.RandomElementByWeight((SitePartDef sp) => sp.selectionWeight);
			Log.Message($"{sitePart}");
			Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[1]
			{
				new SitePartDefWithParams(sitePart, new SitePartParams
				{
					threatPoints = num
				})
			}, tile, faction);
			quest.SpawnWorldObject(site);
			slate.Set("site", site);
			string inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
			string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.NoActiveThreats");
			string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
			quest.Letter(LetterDefOf.PositiveEvent, null, null, label: "DistressSignalLabel".Translate(), text: "DistressSignalText".Translate(site.Faction.Named("FACTION")).Resolve(), lookTargets: Gen.YieldSingle(site), relatedFaction: site.Faction);
			QuestPart_Choice questPart_Choice = quest.RewardChoice();
			QuestPart_Choice.Choice item = new QuestPart_Choice.Choice
			{
				rewards = { (Reward)new Reward_SiteLoot() }
			};
			questPart_Choice.choices.Add(item);
			quest.WorldObjectTimeout(site, 900000);
			quest.Delay(900000, delegate
			{
				QuestGen_End.End(quest, QuestEndOutcome.Fail);
			});
			quest.End(QuestEndOutcome.Success, 0, null, inSignal);
			quest.End(QuestEndOutcome.Fail, 0, null, inSignal2);
		}

		private bool FactionUsable(Faction f, float points)
		{
			if (ModsConfig.RoyaltyActive && points < 2000f && f == Faction.OfEmpire)
			{
				return false;
			}
			if (!f.def.canGenerateQuestSites)
			{
				return false;
			}
			if (f.def.humanlikeFaction && !f.def.pawnGroupMakers.NullOrEmpty())
			{
				return !f.def.permanentEnemy;
			}
			return false;
		}

		private bool TryFindSiteTile(out PlanetTile tile, bool exitOnFirstTileFound = false)
		{
			return TileFinder.TryFindNewSiteTile(out tile, 3, 9, allowCaravans: false, AllowedLandmarks, 0.5f, canSelectComboLandmarks: true, TileFinderMode.Near, exitOnFirstTileFound, validator: t => (t.Tile.hilliness < Hilliness.LargeHills));
		}
	}

	[StaticConstructorOnStartup]
	public class Reward_SiteLoot : Reward
	{
		public string label;

		public string description;

		private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Overlays/QuestionMark");

		public Reward_SiteLoot() { }

		public Reward_SiteLoot(string label, string description)
		{
			this.label = label;
			this.description = description;
		}

		public override IEnumerable<GenUI.AnonymousStackElement> StackElements
		{
			get
			{
				yield return QuestPartUtility.GetStandardRewardStackElement(label, Icon, () => GetDescription(default(RewardsGeneratorParams)).CapitalizeFirst() + ".");
			}
		}

		public override string GetDescription(RewardsGeneratorParams parms)
		{
			return description;
		}

		public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
		{
			throw new NotImplementedException();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref label, "label");
			Scribe_Values.Look(ref description, "description");
		}
	}
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;
using static NAT.CompProperties_ConverterSubject;
using static Verse.ThingFilterUI;

namespace NAT
{
	public class ITab_Converter : ITab
	{
		private static readonly CachedTexture DropTex = new CachedTexture("UI/Buttons/Drop");

		private static readonly Vector2 WinSize = new Vector2(750f, 450f);

		public Building_Converter Converter => base.SelObject as Building_Converter;

		public static List<ThingDef> allDefs = new List<ThingDef>();

		private float viewHeight;

		private float lastDrawnHeight;

		private Vector2 scrollPosition;

		private Vector2 mainScrollPosition;

		private float widthOffset = 0;

		private bool notifyOwner = false;

		public ITab_Converter()
		{
			size = WinSize;
			labelKey = "TabCasketContents";
		}

		public override void OnOpen()
		{
			base.OnOpen();
			allDefs = new List<ThingDef>(CompProperties_ConverterSubject.subjectDefs);
			for (int i = 0; i < CompProperties_ConverterSubject.subjectDefCategories.Count; i++)
			{
				allDefs.AddRange(CompProperties_ConverterSubject.subjectDefCategories[i].subjectDefs);
			}
		}

		public override void TabUpdate()
		{
			base.TabUpdate();
			if (Converter.range < GenRadial.MaxRadialPatternRadius)
			{
				GenDraw.DrawRadiusRing(Converter.Position, Converter.range);
			}
		}

		protected override void FillTab()
		{
			Rect outRect = new Rect(0f, 20f, size.x - 350f, size.y - 20f).ContractedBy(10f);
			Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(lastDrawnHeight, outRect.height));
			Text.Font = GameFont.Small;
			Widgets.BeginScrollView(outRect, ref mainScrollPosition, rect);
			float curY = 0f;
			DoItemsLists(rect, ref curY);
			lastDrawnHeight = curY;
			Widgets.EndScrollView();
			Rect settingsRect = new Rect(outRect.xMax, 20f, 350f, size.y - 20f);
			Widgets.BeginGroup(settingsRect);
			DoSettings(settingsRect);
			Widgets.EndGroup();
		}

		private void DoItemsLists(Rect inRect, ref float curY)
		{
			GUI.BeginGroup(inRect);
			notifyOwner = true;
			ListContainedItems(inRect, Converter.innerContainer, ref curY, "ContainedItems");
			notifyOwner = false;
			ListContainedItems(inRect, Converter.Comp.productsContainer, ref curY, "Products");
			GUI.EndGroup();
		}

		private void ListContainedItems(Rect inRect, ThingOwner innerContainer, ref float curY, string labelKey)
		{
			float num = curY;
			Widgets.ListSeparator(ref curY, inRect.width, labelKey.Translate());
			Rect rect = new Rect(0f, num, inRect.width, curY - num - 3f);
			bool flag = false;
			for (int i = 0; i < innerContainer.Count; i++)
			{
				Thing thing = innerContainer[i];
				DoRow(thing, inRect.width, i, ref curY, innerContainer);
				flag = true;
			}
			if (!flag)
			{
				Widgets.NoneLabel(ref curY, inRect.width);
			}
		}

		private void DoRow(Thing thing, float width, int i, ref float curY, ThingOwner innerContainer)
		{
			Rect rect = new Rect(0f, curY, width, 28f);
			Widgets.InfoCardButton(0f, curY, thing);
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlightSelected(rect);
			}
			else if (i % 2 == 1)
			{
				Widgets.DrawLightHighlight(rect);
			}
			Rect rect2 = new Rect(rect.width - 24f, curY, 24f, 24f);
			if (Widgets.ButtonImage(rect2, DropTex.Texture))
			{
				if (!Converter.OccupiedRect().AdjacentCells.Where((IntVec3 x) => x.Walkable(Converter.Map)).TryRandomElement(out var result))
				{
					result = Converter.Position;
				}
				innerContainer.TryDrop(thing, result, Converter.Map, ThingPlaceMode.Near, thing.stackCount, out var resultingThing);
				if (notifyOwner)
				{
					Converter.Notify_ThingDropped();
				}
				if (resultingThing.TryGetComp(out CompForbiddable comp))
				{
					comp.Forbidden = false;
				}
			}
			TooltipHandler.TipRegionByKey(rect2, "DropThing");
			Widgets.ThingIcon(new Rect(24f, curY, 28f, 28f), thing);
			Rect rect3 = new Rect(60f, curY, rect.width - 36f, rect.height);
			rect3.xMax = rect2.xMin;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect3, thing.LabelCap.Truncate(rect3.width));
			Text.Anchor = TextAnchor.UpperLeft;
			if (Mouse.IsOver(rect))
			{
				TargetHighlighter.Highlight(thing, arrow: true, colonistBar: false);
				TooltipHandler.TipRegion(rect, thing.DescriptionDetailed);
			}
			curY += 28f;
		}

		private void DoSettings(Rect mainRect)
		{
			DoThingFilterConfig(new Rect(0f, 0f, 350f, mainRect.height - 55f).ContractedBy(10f), Converter);

			Rect rect = new Rect(0f, mainRect.height - 70f, 350f, 70f).ContractedBy(10f);
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(rect);
			string text = "IngredientSearchRadius".Translate().Truncate(rect.width * 0.6f);
			string text2 = ((Converter.range == 999) ? "Unlimited".TranslateSimple().Truncate(rect.width * 0.3f) : Converter.range.ToString("F0"));
			listing_Standard.Label(text + ": " + text2);
			Converter.range = Mathf.RoundToInt(listing_Standard.Slider((Converter.range > 100f) ? 100f : Converter.range, 3f, 100f));
			if (Converter.range >= 100)
			{
				Converter.range = 999;
			}
			listing_Standard.End();
		}

		public void DoThingFilterConfig(Rect rect, Building_Converter converter)
		{
			Widgets.DrawMenuSection(rect);
			Widgets.BeginGroup(rect);
			widthOffset = 0;
			Rect rect2 = new Rect(3f, 3f, (rect.width - 12f) / 3f , 24f);
			if (Widgets.ButtonText(rect2, "ClearAll".Translate()))
			{
				converter.allowedDefs.Clear();
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
			}
			if (Widgets.ButtonText(new Rect(rect2.xMax + 3f, rect2.y, rect2.width, 24f), "AllowAll".Translate()))
			{
				converter.allowedDefs = new List<ThingDef>(allDefs);
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
			}
			if (Widgets.ButtonText(new Rect(rect2.xMax + rect2.width + 6f, rect2.y, rect2.width, 24f), "Reset".Translate()))
			{
				converter.ResetAllowedDefs();
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
			}

			Rect outRect = new Rect(0f, 27f, rect.width, rect.height - 27f).ContractedBy(3);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			float curY = 0f;
			
			foreach(ThingDef def in CompProperties_ConverterSubject.subjectDefs)
			{
				DoThingDefRow(def, viewRect.width, ref curY);
			}
			foreach (ConverterSubjectCategory category in CompProperties_ConverterSubject.subjectDefCategories)
			{
				DoCategoryRow(category, viewRect.width, ref curY);
			}
			widthOffset = 0;
			if (Event.current.type == EventType.Layout)
			{
				viewHeight = curY + 20f;
			}
			Widgets.EndScrollView();
			Widgets.EndGroup();
		}

		public void DoThingDefRow(ThingDef def, float width, ref float curY)
		{
			bool flag = !Find.HiddenItemsManager.Hidden(def);
			if (flag)
			{
				Rect infoRect = new Rect(widthOffset, curY, 24f, 24f);
				Rect iconRect = new Rect(widthOffset + 24f, curY, 24f, 24f);
				Widgets.InfoCardButton(infoRect, def);
				Widgets.DefIcon(iconRect, def);
			}
			TaggedString taggedString = flag ? def.LabelCap : "UndiscoveredItemLabel".Translate();
			string tipText = flag ? def.DescriptionDetailed : "UndiscoveredItemDesc".Translate().Resolve();
			LabelLeft(taggedString, tipText, width - 72f - widthOffset, ref curY);
			if (flag)
			{
				bool checkOn = Converter.allowedDefs.Contains(def);
				bool checkOnOld = checkOn;
				Widgets.Checkbox(new Vector2(width - 24f, curY), ref checkOn);
				if (checkOn != checkOnOld)
				{
					if (checkOn)
					{
						Converter.allowedDefs.Add(def);
					}
					else
					{
						Converter.allowedDefs.Remove(def);
					}
				}
			}
			curY += 24f;
		}

		public void DoCategoryRow(ConverterSubjectCategory category, float width, ref float curY)
		{
			bool flag = category.subjectDefs.Any(x => !Find.HiddenItemsManager.Hidden(x));
			if (flag)
			{
				Rect butRect = new Rect(3, curY + 3, 18f, 18f);
				Texture2D tex = (category.fullView ? TexButton.Collapse : TexButton.Reveal);
				if (Widgets.ButtonImage(butRect, tex))
				{
					if (category.fullView)
					{
						SoundDefOf.TabClose.PlayOneShotOnCamera();
					}
					else
					{
						SoundDefOf.TabOpen.PlayOneShotOnCamera();
					}
					category.fullView = !category.fullView;
				}
			}
			TaggedString taggedString = flag ? category.key.Translate() : "UndiscoveredItemLabel".Translate();
			string tipText = (flag ? category.key.Translate() : "UndiscoveredItemDesc".Translate()).Resolve();
			widthOffset = -24f;
			LabelLeft(taggedString, tipText, width - 48f, ref curY);
			widthOffset = 0;
			if (flag)
			{
				int count = 0;
				for (int i = 0; i < category.subjectDefs.Count; i++)
				{
					if (Converter.allowedDefs.Contains(category.subjectDefs[i]))
					{
						count++;
					}
				}
				MultiCheckboxState multiCheckboxState = count == 0 ? MultiCheckboxState.Off : (count == category.subjectDefs.Count ? MultiCheckboxState.On : MultiCheckboxState.Partial);
				MultiCheckboxState multiCheckboxState2 = Widgets.CheckboxMulti(new Rect(width - 24f, curY, 24f, 24f), multiCheckboxState, paintable: true);
				if (multiCheckboxState != multiCheckboxState2)
				{
					if(multiCheckboxState2 == MultiCheckboxState.On)
					{
						for (int i = 0; i < category.subjectDefs.Count; i++)
						{
							if (!Converter.allowedDefs.Contains(category.subjectDefs[i]))
							{
								Converter.allowedDefs.Add(category.subjectDefs[i]);
							}
						}
					}
					else if (multiCheckboxState2 == MultiCheckboxState.Off)
					{
						for (int i = 0; i < category.subjectDefs.Count; i++)
						{
							if (Converter.allowedDefs.Contains(category.subjectDefs[i]))
							{
								Converter.allowedDefs.Remove(category.subjectDefs[i]);
							}
						}
					}
				}
			}
			curY += 24f;
			if (category.fullView)
			{
				widthOffset = 12;
				foreach (ThingDef def in category.subjectDefs)
				{
					DoThingDefRow(def, width, ref curY);
				}
				widthOffset = 0;
			}
		}

		protected void LabelLeft(string label, string tipText, float width, ref float curY)
		{
			Rect rect = new Rect(widthOffset + 48f, curY, width + 24f, 24f);
			Widgets.DrawHighlightIfMouseover(rect);
			if (!tipText.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					GUI.DrawTexture(rect, TexUI.HighlightTex);
				}
				TooltipHandler.TipRegion(rect, tipText);
			}
			Verse.Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect, label.Truncate(rect.width));
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}

	public class ITab_ContentsConverter : ITab_ContentsBase
	{
		

		public Building_Converter Converter => SelThing as Building_Converter;

		public override IList<Thing> container => Converter.GetDirectlyHeldThings().Concat(Converter.Comp.GetDirectlyHeldThings()).ToList();

		public override bool IsVisible => true;

		

		
	}
}

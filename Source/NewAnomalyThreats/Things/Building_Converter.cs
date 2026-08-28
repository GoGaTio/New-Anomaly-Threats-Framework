using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.Code;
using static RimWorld.Building_HoldingPlatform;

namespace NAT
{
	[StaticConstructorOnStartup]
	public class Building_Converter : Building, IThingHolder, ISearchableContents, Interfaces
	{
		private static readonly Material UnfilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.65f), ShaderDatabase.MetaOverlay);

		private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f, 0.65f), ShaderDatabase.MetaOverlay);

		private static readonly Vector2 BarSize = new Vector2(0.8f, 0.1f);

		private CompConverter cachedComp;

		public CompConverter Comp => cachedComp ?? (cachedComp = GetComp<CompConverter>());

		[Unsaved(false)]
		private CompPowerTrader cachedPowerComp;

		public CompPowerTrader PowerTraderComp
		{
			get
			{
				if (cachedPowerComp == null)
				{
					cachedPowerComp = this.TryGetComp<CompPowerTrader>();
				}
				return cachedPowerComp;
			}
		}

		public ThingOwner<Thing> innerContainer;

		public int ticksWorkingLeft = -1;

		public int ticksTillCheck;

		public int range = 999;

		public bool dropAlways = false;

		public ThingDef workingDef;

		public List<ThingDef> allowedDefs = new List<ThingDef>();

		public ThingOwner SearchableContents => innerContainer;

		public bool NeedDelivery
		{
			get
			{
				if (allowedDefs.NullOrEmpty())
				{
					return false;
				}
				if(innerContainer.Count > 4)
				{
					return false;
				}
				if (!PowerTraderComp.PowerOn)
				{
					return false;
				}
				return true;
			}
		}

		public Building_Converter()
		{
			innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
		}

		public ThingCount FindItem(Pawn pawn)
		{
			Thing t = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator, CompConverterSubject.subjects.Where(x => x.Map == Map));
			if (t == null)
			{
				return default(ThingCount);
			}
			return new ThingCount(t, t.stackCount);
			bool Validator(Thing x)
			{
				if (range != 999 && Position.DistanceTo(x.Position) > range)
				{
					return false;
				}
				if (!CanAccept(x))
				{
					return false;
				}
				if (x.IsForbidden(pawn) || !pawn.CanReserve(x, 1, x.stackCount))
				{
					return false;
				}
				return true;
			}
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return innerContainer;
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public override void PostPostMake()
		{
			base.PostPostMake();
			ResetAllowedDefs();
		}

		public void ResetAllowedDefs()
		{
			allowedDefs = new List<ThingDef>();
			for (int i = 0; i < CompProperties_ConverterSubject.subjectDefs.Count; i++)
			{
				if (!CompProperties_ConverterSubject.subjectDefs[i].GetCompProperties<CompProperties_ConverterSubject>().allowByDefault)
				{
					continue;
				}
				allowedDefs.Add(CompProperties_ConverterSubject.subjectDefs[i]);
			}
			for (int i = 0; i < CompProperties_ConverterSubject.subjectDefCategories.Count; i++)
			{
				if (!CompProperties_ConverterSubject.subjectDefCategories[i].allowByDefault)
				{
					continue;
				}
				allowedDefs.AddRange(CompProperties_ConverterSubject.subjectDefCategories[i].subjectDefs);
			}
		}

		public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
		{
			if(workingDef != null)
			{
				GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest
				{
					center = drawLoc + Vector3.up * 0.1f,
					size = BarSize,
					fillPercent = 1f - ((float)ticksWorkingLeft / (float)workingDef.GetCompProperties<CompProperties_ConverterSubject>().ticksToConvert),
					filledMat = FilledMat,
					unfilledMat = UnfilledMat,
					margin = 0.15f
				};
				r.rotation = Rot4.North;
				GenDraw.DrawFillableBar(r);
			}
			base.DynamicDrawPhaseAt(phase, drawLoc, flip);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			EjectAllContents();
			base.DeSpawn(mode);
		}

		protected override void Tick()
		{
			base.Tick();
			if(ticksTillCheck > 0)
			{
				ticksTillCheck--;
				return;
			}
			if (!Spawned || !PowerTraderComp.PowerOn)
			{
				ticksTillCheck = 60;
				return;
			}
			if(workingDef == null)
			{
				workingDef = innerContainer.FirstOrDefault()?.def;
				if (workingDef != null && workingDef.GetCompProperties<CompProperties_ConverterSubject>() != null)
				{
					CompProperties_ConverterSubject props = workingDef.GetCompProperties<CompProperties_ConverterSubject>();
					if(props == null)
					{
						innerContainer.TryDrop(innerContainer.First(), Position, Map, ThingPlaceMode.Near, innerContainer.First().stackCount, out var _);
						ticksTillCheck = 30;
						workingDef = null;
						return;
					}
					ticksWorkingLeft = props.ticksToConvert;
				}
				else
				{
					ticksTillCheck = 60;
				}
			}
			else
			{
				ticksWorkingLeft--;
				if(ticksWorkingLeft <= 0)
				{
					Thing thing = innerContainer.FirstOrDefault(x => x.def == workingDef);
					if (thing != null)
					{
						CompConverterSubject comp = thing.TryGetComp<CompConverterSubject>();
						if(comp != null)
						{
							Comp.productsContainer.TryAddRangeOrTransfer(comp.GetProducts());
							foreach (Thing t in Comp.productsContainer.ToList())
							{
								if (dropAlways || t.stackCount >= t.def.stackLimit)
								{
									Comp.productsContainer.TryDrop(t, Position, Map, ThingPlaceMode.Near, t.stackCount, out var resultingThing);
									if (resultingThing.TryGetComp(out CompForbiddable c))
									{
										c.Forbidden = false;
									}
								}
							}
						}
						if (thing.stackCount > 1)
						{
							thing.SplitOff(1).Destroy();
						}
						else
						{
							innerContainer.Remove(thing);
							thing.Destroy();
						}
					}
					Stop();
				}
			}
		}

		public bool CanAccept(Thing thing)
		{
			if (!allowedDefs.Contains(thing.def))
			{
				return false;
			}
			if (innerContainer.CanAcceptAnyOf(thing))
			{
				return true;
			}
			return false;
		}

		public void Notify_ThingDropped()
		{
			if(workingDef == null)
			{
				Stop();
			}
			else if(!innerContainer.Any(x => x.def == workingDef))
			{
				Stop();
			}
		}

		public void Stop()
		{
			ticksWorkingLeft = -1;
			workingDef = null;
		}

		public void EjectAllContents()
		{
			innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
			Comp.productsContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
			Stop();
		}

		public void EjectContents()
		{
			innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
			Stop();
		}

		public void EjectProducts()
		{
			Comp.productsContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			
			if (workingDef != null)
			{
				if (!text.NullOrEmpty())
				{
					text += "\n";
				}
				text += "TimeLeft".Translate().CapitalizeFirst() + ": " + ticksWorkingLeft.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor);
			}
			return text;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksWorkingLeft, "ticksWorkingLeft", forceSave: true);
			Scribe_Values.Look(ref range, "range", forceSave: true);
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_Defs.Look(ref workingDef, "workingDef");
			Scribe_Collections.Look(ref allowedDefs, "allowedDefs", LookMode.Def);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				allowedDefs.RemoveAll((ThingDef x) => x == null);
			}
		}
	}
}

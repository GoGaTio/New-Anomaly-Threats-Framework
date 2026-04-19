using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace NAT
{
	public class EndGameRaidDef : Def
	{
		public class WaveGroupParms
		{
			public WaveGroupParms() { }

			public Type lordJobType;

			public float minPoints;

			public PawnGroupKindDef groupKind;

			public PawnKindDef extraPawnKind;

			public int extraPawnsCount = 1;
		}

		public float commonality = 1f;

		public bool isWave = true;

		public string letterLabel = null;

		public string letterDesc = null;

		public PawnsArrivalModeDef arrivalMode;

		public List<WaveGroupParms> groups = new List<WaveGroupParms>();

		public virtual bool TryFireWave(Map map, float points)
		{
			Log.Message(defName);
			string label = letterLabel ?? "VoidAwakeningEntityArrivalLabel".Translate();
			string desc = letterDesc ?? "VoidAwakeningEntityArrivalText".Translate();
			points = points / groups.Count;
			List<Pawn> list = new List<Pawn>();
			IncidentParms incidentParms = new IncidentParms
			{
				target = map,
				raidArrivalMode = arrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkInDistributedGroups,
				sendLetter = false,
				faction = Faction.OfEntities
			};
			if (!incidentParms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(incidentParms))
			{
				return false;
			}
			foreach (WaveGroupParms parms in groups)
			{
				float localPoints = Mathf.Max(points, Mathf.Max(parms.minPoints, Faction.OfEntities.def.MinPointsToGeneratePawnGroup(parms.groupKind) * 1.05f));
				List<Pawn> localList = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
				{
					groupKind = parms.groupKind,
					points = localPoints,
					faction = Faction.OfEntities
				}).ToList();
				if(parms.extraPawnKind != null)
				{
					for(int i = 0; i < parms.extraPawnsCount; i++)
					{
						localList.Add(PawnGenerator.GeneratePawn(parms.extraPawnKind, Faction.OfEntities));
					}
				}
				list.AddRange(localList);
				LordMaker.MakeNewLord(Faction.OfEntities, parms.lordJobType != null ? Activator.CreateInstance(parms.lordJobType) as LordJob : new LordJob_AssaultColony(incidentParms.faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false, useAvoidGridSmart: false, canSteal: false), map, localList);
			}
			list.Shuffle();
			incidentParms.raidArrivalMode.Worker.Arrive(list, incidentParms);
			Find.LetterStack.ReceiveLetter(label, desc, LetterDefOf.ThreatBig, list);
			return true;
		}
	}
}

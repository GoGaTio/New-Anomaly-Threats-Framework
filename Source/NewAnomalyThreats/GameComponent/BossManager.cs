using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static System.Net.Mime.MediaTypeNames;

namespace NAT
{
	public class AnomalyBossManager : IExposable
	{
		public AnomalyBossManager()
		{

		}

		public List<AnomalyBoss> bosses = new List<AnomalyBoss>();

		public AnomalyBoss GetBoss(AnomalyBossDef def)
		{
			for (int i = 0; i < bosses.Count; i++)
			{
				if (bosses[i].def == def)
				{
					return bosses[i];
				}
			}
			return null;
		}

		public bool AnyBossIncoming
		{
			get
			{
				for (int i = 0; i < bosses.Count; i++)
				{
					if (bosses[i].Incoming)
					{
						return true;
					}
				}
				return false;
			}
		}

		public IEnumerable<AnomalyBossDef> GetIncomingBosses()
		{
			for (int i = 0; i < bosses.Count; i++)
			{
				if (bosses[i].Incoming)
				{
					yield return bosses[i].def;
				}
			}
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref bosses, "bosses", LookMode.Deep);
			if(Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				foreach(AnomalyBoss boss in bosses.ToList())
				{
					if(boss.def == null)
					{
						bosses.Remove(boss);
					}
					else
					{
						boss.Init();
					}
				}
				InitBosses();
			}
		}

		public void Tick()
		{
			for (int num = bosses.Count - 1; num >= 0; num--)
			{
				bosses[num].Tick();
			}
		}

		public void InitBosses()
		{
			foreach (AnomalyBossDef bossDef in DefDatabase<AnomalyBossDef>.AllDefs)
			{
				if (!bosses.Any(x => x.def == bossDef))
				{
					AnomalyBoss boss = (AnomalyBoss)Activator.CreateInstance(bossDef.bossClass);
					boss.def = bossDef;
					boss.Init();
					bosses.Add(boss);
				}
			}
		}
	}
}

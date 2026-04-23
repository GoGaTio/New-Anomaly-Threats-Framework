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
	public class AnomalyBossDef : Def
	{
		public class BossGroup
		{
			public BossGroup() { }

			public string tag;

			public int minIndex = 0;

			public int maxIndex = int.MaxValue;

			public List<PawnGenOption> escorts = new List<PawnGenOption>();

			public bool CanUseNow(int index)
			{
				if(index < minIndex)
				{
					return false;
				}
				if(index > maxIndex)
				{
					return false;
				}
				return true;
			}
		}

		public List<BossGroup> groups = new List<BossGroup>();

		public void 
	}
}

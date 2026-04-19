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
	public class DefGeneratorDef : Def
	{
		public DefGeneratorWorker generator;
	}

	public class DefGeneratorWorker
	{
		public DefGeneratorWorker() { }

		public virtual void AddImpliedDefs(bool hotReload = false)
		{

		}
	}
}

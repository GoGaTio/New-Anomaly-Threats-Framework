using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NAT
{
	public class LordJob_BossRaid : LordJob
	{
		public override bool GuiltyOnDowned => true;

		public LordJob_BossRaid()
		{
		}

		public virtual LordToil AssaultToil()
		{

		}

		public virtual LordToil DamageReactToil()
		{

		}

		public virtual int TicksToStopDamageReact => 5000;

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();
			List<LordToil> list = new List<LordToil>();
			LordToil lordToil_Assault = AssaultToil();
			stateGraph.AddToil(lordToil_Assault);

			LordToil lordToil_DamageReact = DamageReactToil();
			stateGraph.AddToil(lordToil_DamageReact);

			Transition transition1 = new Transition(lordToil_Assault, lordToil_DamageReact);
			transition1.AddTrigger(new Trigger_PawnHarmed(1f, requireInstigatorWithFaction: false));
			stateGraph.AddTransition(transition1);

			Transition transition2 = new Transition(lordToil_DamageReact, lordToil_Assault);
			transition2.AddTrigger(new Trigger_TicksPassedWithoutHarm(TicksToStopDamageReact));
			stateGraph.AddTransition(transition2);

			return stateGraph;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref stageLoc, "stageLoc");
			Scribe_Values.Look(ref fractionLostToAssault, "fractionLostToAssault", defaultValue: 0.05f);
			Scribe_Values.Look(ref waitForever, "waitForever", defaultValue: false);
			Scribe_Values.Look(ref canKidnap, "canKidnap", defaultValue: true);
			Scribe_Values.Look(ref canTimeoutOrFlee, "canTimeoutOrFlee", defaultValue: true);
			Scribe_Values.Look(ref canLeave, "canLeave", defaultValue: true);
			Scribe_Values.Look(ref breachers, "breaching", defaultValue: false);
			Scribe_Values.Look(ref canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", defaultValue: false);
		}
	}
}

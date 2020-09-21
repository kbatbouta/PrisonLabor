using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Verse.AI.Group;
using System.Linq;
using HarmonyLib;
using System.Reflection;

namespace AndroidTiers
{
    public class CompBuildingSkyMindRelay : ThingComp
    {
        public override void PostPostMake()
        {
            base.PostPostMake();
            base.parent.ATCompBuildingSkyMindRelay = this;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);


            Utils.GCATPP.popRelayTower((Building)this.parent, map);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            Utils.GCATPP.popRelayTower((Building)this.parent, previousMap);
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            Building build = (Building)this.parent;

            switch(signal) {
                case "PowerTurnedOn":
                    Utils.GCATPP.pushRelayTower(build);
                    break;
                case "PowerTurnedOff":
                    Utils.GCATPP.popRelayTower(build, build.Map);
                    break;
            }
        }


        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            base.parent.ATCompBuildingSkyMindRelay = this;
            if(this.parent.TryGetComp<CompPowerTrader>().PowerOn)
                Utils.GCATPP.pushRelayTower((Building)this.parent);
        }
    }
}
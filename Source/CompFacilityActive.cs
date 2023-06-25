using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GrowthAccelerator;
public class CompFacilityActive : ThingComp {
    [Unsaved(false)]
    private Effecter effecterInUse;

    public CompProperties_FacilityInUse Props => props as CompProperties_FacilityInUse;

    public override void PostDeSpawn(Map map) {
        base.PostDeSpawn(map);
        effecterInUse?.Cleanup();
        effecterInUse = null;
    }

    public override void CompTick() {
        base.CompTick();
        DoTick();
    }

    public override void CompTickRare() {
        base.CompTickRare();
        DoTick();
    }

    public override void CompTickLong() {
        base.CompTickLong();
        DoTick();
    }

    private void DoTick() {
        List<Thing> list = parent.TryGetComp<CompFacility>()?.LinkedBuildings;
        if (list == null) {
            return;
        }

        CompPowerTrader compPowerTrader = parent.TryGetComp<CompPowerTrader>();
        Thing thing = null;
        foreach (Thing item in list) {
            if (compPowerTrader.PowerOn && BuildingInUse(item)) {
                thing = item;
                break;
            }
        }

        bool on = thing != null;

        if (parent.IsHashIntervalTick(250)) {
            var comp = parent.TryGetComp<CompPowerTrader>();
            if (comp != null) {
                comp.PowerOutput = on ? (0f - comp.Props.PowerConsumption) : (0f - comp.Props.idlePowerDraw);
            }
        }

        if (Props.effectInUse == null) return;
        if (on) {
            if (effecterInUse == null) {
                effecterInUse = Props.effectInUse.Spawn();
                effecterInUse.Trigger(parent, thing);
            }

            effecterInUse.EffectTick(parent, thing);
        }
        if (!on && effecterInUse != null) {
            effecterInUse.Cleanup();
            effecterInUse = null;
        }
    }

    private bool BuildingInUse(Thing building) 
        => (building.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)
        && ((building as Building_Enterable)?.Working ?? true);
}

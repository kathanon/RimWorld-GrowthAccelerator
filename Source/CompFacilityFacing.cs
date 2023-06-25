using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GrowthAccelerator;
[HarmonyPatch]
public class CompFacilityFacing : CompFacility {

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CompAffectedByFacilities), 
                  nameof(CompAffectedByFacilities.CanPotentiallyLinkTo_Static), 
                  typeof(ThingDef), typeof(IntVec3), typeof(Rot4), typeof(ThingDef), typeof(IntVec3), typeof(Rot4))]
    public static void CanPotentiallyLinkTo_Static_Post(
            ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, 
            ThingDef myDef, IntVec3 myPos, Rot4 myRot, 
            ref bool __result) {
        if (!__result) return;
        var props = facilityDef.GetCompProperties<CompProperties_Facility>();
        if (props?.compClass != typeof(CompFacilityFacing)) return;

        var me = GenAdj.OccupiedRect(myPos, myRot, myDef.size);
        var facility = GenAdj.OccupiedRect(facilityPos, facilityRot, facilityDef.size);
        var moved = facility.MovedBy(facilityRot.FacingCell);
        __result = me.Overlaps(moved);
    }
}



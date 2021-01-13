using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace WeaponReloading
{
    internal class JobGiver_ReloadWeapon : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return 5.9f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            var comp = FindAnyReloadableWeapon(pawn);
            if (comp == null) return null;
            if (!comp.NeedsReload()) return null;
            var list = FindAmmo(pawn, pawn.Position, comp);
            return list.NullOrEmpty() ? null : MakeReloadJob(comp, list);
        }

        public static Job MakeReloadJob(CompReloadableWeapon comp, List<Thing> ammo)
        {
            var job = JobMaker.MakeJob(ReloadDefOf.ReloadWeapon, comp.parent);
            job.targetQueueB = ammo.Select(t => new LocalTargetInfo(t)).ToList();
            job.count = Math.Min(ammo.Sum(t => t.stackCount), comp.Props.MaxShots - comp.ShotsRemaining);
            return job;
        }

        public static List<Thing> FindAmmo(Pawn pawn, IntVec3 root, CompReloadableWeapon comp)
        {
            if (comp == null) return null;
            var desired = new IntRange(comp.Props.ItemsPerShot,
                comp.Props.ItemsPerShot * (comp.Props.MaxShots - comp.ShotsRemaining));
            return RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, root, desired, comp.CanReloadFrom);
        }

        public static CompReloadableWeapon FindAnyReloadableWeapon(Pawn pawn)
        {
            if (pawn?.equipment == null) return null;
            foreach (var thing in pawn.equipment.AllEquipmentListForReading)
                if (thing.TryGetComp<CompReloadableWeapon>() is CompReloadableWeapon comp)
                    return comp;
            return null;
        }
    }
}
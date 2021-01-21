using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace WeaponReloading
{
    internal class JobGiver_ReloadFromInventory : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return 6.1f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            var comp = JobGiver_ReloadWeapon.FindAnyReloadableWeapon(pawn);
            if (comp == null) return null;
            if (!comp.NeedsReload()) return null;
            return (from thing in pawn.inventory.GetDirectlyHeldThings()
                where comp.CanReloadFrom(thing)
                select MakeReloadJob(comp, thing)).FirstOrDefault();
        }

        public static Job MakeReloadJob(CompReloadableWeapon comp, Thing ammo)
        {
            var job = JobMaker.MakeJob(ReloadDefOf.ReloadWeaponFromInventory, comp.parent);
            job.targetB = ammo;
            job.count = Math.Min(ammo.stackCount,
                comp.Props.ItemsPerShot * (comp.Props.MaxShots - comp.ShotsRemaining));
            return job;
        }
    }
}
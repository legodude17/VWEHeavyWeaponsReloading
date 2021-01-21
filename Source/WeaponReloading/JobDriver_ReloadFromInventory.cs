using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace WeaponReloading
{
    internal class JobDriver_ReloadFromInventory : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var thing = job.targetA.Thing;
            var comp = thing?.TryGetComp<CompReloadableWeapon>();

            this.FailOn(() => JobDriver_ReloadWeapon.GetHolder(thing) != pawn);
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            var reloadTicks = 0;
            var done = Toils_General.Label();

            yield return Toils_Jump.JumpIf(done, () => comp == null || pawn.carryTracker.CarriedThing != null ||
                                                       !comp.NeedsReload());
            var toil = new Toil
            {
                initAction = () =>
                {
                    var item = job.targetB.Thing;
                    pawn.inventory.innerContainer.TryTransferToContainer(item, pawn.carryTracker.innerContainer,
                        job.count);
                    reloadTicks = comp.ReloadTicks(pawn.carryTracker.CarriedThing);
                },
                defaultCompleteMode = ToilCompleteMode.Never,
                defaultDuration = comp.Props.ReloadTimePerShot.SecondsToTicks(),
                tickAction = () =>
                {
                    if (debugTicksSpentThisToil >= reloadTicks)
                    {
                        comp.Reload(pawn.carryTracker.CarriedThing);
                        JumpToToil(done);
                    }
                }
            };
            toil.WithProgressBar(TargetIndex.A, () => (float) debugTicksSpentThisToil / (float) reloadTicks);
            yield return toil;

            yield return done;
        }
    }
}
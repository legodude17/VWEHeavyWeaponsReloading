using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace WeaponReloading
{
    public class JobDriver_ReloadWeapon : JobDriver
    {
        private Sustainer reloadSound;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var thing = job.GetTarget(TargetIndex.A).Thing;
            var comp = thing?.TryGetComp<CompReloadableWeapon>();

            this.FailOn(() => comp == null);
            this.FailOn(() => GetHolder(thing) != pawn);
            this.FailOn(() => !comp.NeedsReload());
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

            var getNextIngredient = Toils_General.Label();
            yield return getNextIngredient;
            foreach (var toil in ReloadFromCarried(comp)) yield return toil;
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true);
            yield return Toils_Jump.JumpIf(getNextIngredient, () => !job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
            foreach (var toil in ReloadFromCarried(comp)) yield return toil;

            yield return new Toil
            {
                initAction = () =>
                {
                    var t = pawn.carryTracker.CarriedThing;
                    if (t != null && !t.Destroyed)
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var dropped);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private IEnumerable<Toil> ReloadFromCarried(CompReloadableWeapon comp)
        {
            var done = Toils_General.Label();
            yield return Toils_Jump.JumpIf(done,
                () => pawn.carryTracker.CarriedThing == null || !comp.CanReloadFrom(pawn.carryTracker.CarriedThing));
            var reloadTicks = 0;
            var toil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                defaultDuration = comp.Props.ReloadTimePerShot.SecondsToTicks(),
                initAction = () =>
                {
                    reloadTicks = comp.ReloadTicks(pawn.carryTracker.CarriedThing);
                    reloadSound = null;
                },
                tickAction = () =>
                {
                    if (debugTicksSpentThisToil >= reloadTicks)
                    {
                        comp.Reload(pawn.carryTracker.CarriedThing)?.Destroy();
                        JumpToToil(done);
                    }

                    if (debugTicksSpentThisToil == reloadTicks - 2f.SecondsToTicks())
                        comp.Props.ReloadSound?.PlayOneShot(pawn);

                    reloadSound?.Maintain();
                }
            };
            toil.WithProgressBar(TargetIndex.A, () => (float) debugTicksSpentThisToil / (float) reloadTicks);
            yield return toil;
            yield return done;
            yield return Toils_General.Do(() => reloadSound?.End());
        }

        public static Pawn GetHolder(Thing thing)
        {
            var eq = thing.holdingOwner.Owner as Pawn_EquipmentTracker;
            return eq?.pawn;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WeaponReloading
{
    public class WeaponReloadingMod : Mod
    {
        private static FieldInfo thisPropertyInfo;

        public WeaponReloadingMod(ModContentPack content) : base(content)
        {
            var harm = new Harmony("legodude17.weaponreloading");
            harm.Patch(
                AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
                postfix: new HarmonyMethod(AccessTools.Method(GetType(), "AddWeaponReloadOrders")));
            harm.Patch(AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand"),
                new HarmonyMethod(AccessTools.Method(GetType(), "CreateReloadableVerbTargetCommand")));
            var type = typeof(JobDriver_AttackStatic).GetNestedType("<>c__DisplayClass4_0", BindingFlags.NonPublic);
            thisPropertyInfo = type.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            harm.Patch(type.GetMethod("<MakeNewToils>b__1", BindingFlags.NonPublic | BindingFlags.Instance),
                transpiler: new HarmonyMethod(AccessTools.Method(GetType(), "EndJobIfVerbNotAvailable")));
            harm.Patch(AccessTools.Method(typeof(Stance_Busy), "Expire"),
                postfix: new HarmonyMethod(GetType(), "ReloadWeaponIfEndingCooldown"));
            Log.Message("Applied patches for " + harm.Id);
        }

        public static void AddWeaponReloadOrders(List<FloatMenuOption> opts, Vector3 clickPos, Pawn pawn)
        {
            var c = IntVec3.FromVector3(clickPos);
            foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                if (eq.TryGetComp<CompReloadableWeapon>() is CompReloadableWeapon comp)
                    foreach (var thing in c.GetThingList(pawn.Map))
                        if (comp.CanReloadFrom(thing))
                        {
                            var text = "Reload".Translate(comp.parent.Named("GEAR"),
                                           thing.def.Named("AMMO")) + " (" + comp.ShotsRemaining + "/" +
                                       comp.Props.MaxShots + ")";
                            var failed = false;
                            var ammo = new List<Thing>();
                            if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                text += ": " + "NoPath".Translate().CapitalizeFirst();
                                failed = true;
                            }
                            else if (!comp.NeedsReload())
                            {
                                text += ": " + "ReloadFull".Translate();
                                failed = true;
                            }
                            else if ((ammo = JobGiver_ReloadWeapon.FindAmmo(pawn, c, comp)).NullOrEmpty())
                            {
                                text += ": " + "ReloadNotEnough".Translate();
                                failed = true;
                            }

                            if (failed) opts.Add(new FloatMenuOption(text, null));
                            else
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                                    new FloatMenuOption(text,
                                        () => pawn.jobs.TryTakeOrderedJob(
                                            JobGiver_ReloadWeapon.MakeReloadJob(comp, ammo))), pawn, thing));
                        }
        }

        public static bool CreateReloadableVerbTargetCommand(Thing ownerThing, Verb verb,
            ref Command_VerbTarget __result)
        {
            if (verb is Verb_ShootReloadable verbReloadable)
            {
                var comp = ownerThing.TryGetComp<CompReloadableWeapon>();
                var command = new Command_ReloadableWeapon(comp)
                {
                    defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst(),
                    icon = ownerThing.def.uiIcon,
                    iconAngle = ownerThing.def.uiIconAngle,
                    iconOffset = ownerThing.def.uiIconOffset,
                    tutorTag = "VerbTarget",
                    verb = verb
                };

                if (verb.caster.Faction != Faction.OfPlayer)
                    command.Disable("CannotOrderNonControlled".Translate());
                else if (verb.CasterIsPawn && verb.CasterPawn.WorkTagIsDisabled(WorkTags.Violent))
                    command.Disable(
                        "IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                else if (verb.CasterIsPawn && !verb.CasterPawn.drafter.Drafted)
                    command.Disable(
                        "IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                else if (comp.ShotsRemaining < verb.verbProps.burstShotCount)
                    command.Disable("CommandReload_NoAmmo".Translate("ammo".Named("CHARGENOUN"),
                        comp.Props.AmmoFilter.AnyAllowedDef.Named("AMMO"),
                        ((comp.Props.MaxShots - comp.ShotsRemaining) * comp.Props.ItemsPerShot).Named("COUNT")));

                __result = command;

                return false;
            }

            return true;
        }

        public static IEnumerable<CodeInstruction> EndJobIfVerbNotAvailable(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var list = instructions.ToList();
            var idx = list.FindIndex(ins => ins.IsLdarg(0));
            var idx2 = list.FindIndex(idx + 1, ins => ins.IsLdarg(0));
            var idx3 = list.FindIndex(idx2, ins => ins.opcode == OpCodes.Ret);
            var list2 = list.Skip(idx2).Take(idx3 - idx2).ToList().ListFullCopy();
            list2.Find(ins => ins.opcode == OpCodes.Ldc_I4_2).opcode = OpCodes.Ldc_I4_3;
            var idx4 = list.FindIndex(ins => ins.opcode == OpCodes.Stloc_2);
            var label = generator.DefineLabel();
            list[idx4 + 1].labels.Add(label);
            var list3 = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, thisPropertyInfo),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(JobDriver), "pawn")),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(WeaponReloadingMod), "PawnCanCurrentlyUseVerb")),
                new CodeInstruction(OpCodes.Brtrue_S, label)
            };
            list3.AddRange(list2);
            list.InsertRange(idx4 + 1, list3);
            return list;
        }

        public static bool PawnCanCurrentlyUseVerb(Verb verb, Pawn pawn)
        {
            return verb.IsMeleeAttack
                ? verb.CanHitTargetFrom(pawn.Position, verb.CurrentTarget)
                : verb.IsStillUsableBy(pawn);
        }

        public static void ReloadWeaponIfEndingCooldown(Stance_Busy __instance)
        {
            if (__instance.verb?.EquipmentSource == null) return;
            var pawn = __instance.verb.CasterPawn;
            if (pawn == null) return;
            var comp = __instance.verb.EquipmentSource.TryGetComp<CompReloadableWeapon>();
            if (comp == null || comp.ShotsRemaining != 0 || pawn.CurJobDef != JobDefOf.AttackStatic ||
                pawn.stances.curStance.StanceBusy) return;

            var item = pawn.inventory.innerContainer.FirstOrDefault(t => comp.CanReloadFrom(t));

            if (item == null) return;

            var job = new Job(JobDefOf.AttackStatic, pawn.CurJob.targetA);
            job.canUseRangedWeapon = true;
            job.verbToUse = __instance.verb;
            job.endIfCantShootInMelee = true;
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            pawn.jobs.TryTakeOrderedJob_NewTemp(JobGiver_ReloadFromInventory.MakeReloadJob(comp, item),
                requestQueueing: true);
            pawn.jobs.TryTakeOrderedJob_NewTemp(job, JobTag.DraftedOrder, true);
        }
    }
}
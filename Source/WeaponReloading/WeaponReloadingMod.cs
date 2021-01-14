using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WeaponReloading
{
    public class WeaponReloadingMod : Mod
    {
        public WeaponReloadingMod(ModContentPack content) : base(content)
        {
            var harm = new Harmony("legodude17.weaponreloading");
            harm.Patch(
                AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
                postfix: new HarmonyMethod(AccessTools.Method(GetType(), "AddWeaponReloadOrders")));
            harm.Patch(AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand"),
                new HarmonyMethod(AccessTools.Method(GetType(), "CreateReloadableVerbTargetCommand")));
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
    }
}
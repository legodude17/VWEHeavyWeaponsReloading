using System;
using Verse;

namespace WeaponReloading
{
    public class CompReloadableWeapon : ThingComp
    {
        public int ShotsRemaining;

        public CompProperties_ReloadableWeapon Props => props as CompProperties_ReloadableWeapon;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ShotsRemaining = Props.MaxShots;
        }

        public virtual void Reload(Thing ammo)
        {
            if (!CanReloadFrom(ammo)) return;
            var shotsToFill = ShotsToReload(ammo);
            ammo.SplitOff(shotsToFill * Props.ItemsPerShot);
            ShotsRemaining += shotsToFill;
        }

        public virtual int ReloadTicks(Thing ammo)
        {
            Log.Message("ammo: " + ammo);
            return ammo == null ? 0 : (Props.ReloadTimePerShot * ShotsToReload(ammo)).SecondsToTicks();
        }

        private int ShotsToReload(Thing ammo)
        {
            return Math.Min(ammo.stackCount / Props.ItemsPerShot, Props.MaxShots - ShotsRemaining);
        }

        public virtual bool NeedsReload()
        {
            return ShotsRemaining < Props.MaxShots;
        }

        public virtual bool CanReloadFrom(Thing ammo)
        {
            return Props.AmmoFilter.Allows(ammo) && ammo.stackCount >= Props.ItemsPerShot;
        }

        public virtual bool CanReloadFrom(ThingDef ammo)
        {
            return Props.AmmoFilter.Allows(ammo);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ShotsRemaining, "ShotsRemaining");
        }
    }

    public class CompProperties_ReloadableWeapon : CompProperties
    {
        public ThingFilter AmmoFilter;
        public int ItemsPerShot;
        public int MaxShots;
        public float ReloadTimePerShot;

        public CompProperties_ReloadableWeapon()
        {
            compClass = typeof(CompReloadableWeapon);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            AmmoFilter.ResolveReferences();
        }
    }
}
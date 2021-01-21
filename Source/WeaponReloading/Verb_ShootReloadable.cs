using Verse;

namespace WeaponReloading
{
    public class Verb_ShootReloadable : Verb_Shoot
    {
        public override ThingDef Projectile
        {
            get
            {
                if (EquipmentSource != null &&
                    EquipmentSource.TryGetComp<CompChangeableWeaponAmmo>() is CompChangeableWeaponAmmo comp &&
                    comp.CurrentProjectile != null)
                    return comp.CurrentProjectile;

                return base.Projectile;
            }
        }

        protected override bool TryCastShot()
        {
            var comp = EquipmentSource.TryGetComp<CompReloadableWeapon>();
            if (comp == null) return false;
            var flag = base.TryCastShot();
            comp.Notify_ProjectileFired();
            return flag;
        }

        public override bool Available()
        {
            var comp = EquipmentSource.TryGetComp<CompReloadableWeapon>();
            if (comp == null) return false;
            return comp.ShotsRemaining > 0 && base.Available();
        }
    }
}
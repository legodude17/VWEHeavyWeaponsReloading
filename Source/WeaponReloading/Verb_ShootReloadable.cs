using Verse;

namespace WeaponReloading
{
    public class Verb_ShootReloadable : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            var comp = EquipmentSource.TryGetComp<CompReloadableWeapon>();
            if (comp == null) return false;
            var flag = base.TryCastShot();
            comp.ShotsRemaining--;
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
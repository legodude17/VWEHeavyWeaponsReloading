using Verse;

namespace WeaponReloading
{
    public class Command_ReloadableWeapon : Command_VerbTarget
    {
        public CompReloadableWeapon comp;

        public Command_ReloadableWeapon(CompReloadableWeapon comp)
        {
            this.comp = comp;
        }

        public override string TopRightLabel => comp.ShotsRemaining + " / " + comp.Props.MaxShots;
    }
}
using System.Collections.Generic;
using System.Linq;
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

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach (var option in base.RightClickFloatMenuOptions) yield return option;

                if (comp is CompChangeableWeaponAmmo ccwa)
                    foreach (var option in ccwa.AmmoOptions.Select(pair =>
                        new FloatMenuOption(pair.First.LabelCap, pair.Second)))
                        yield return option;
            }
        }
    }
}
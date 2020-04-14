using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXILED.Extensions;
using Grenades;
using Harmony;

namespace BetterFriendlyFire
{
    [HarmonyPatch(typeof(FragGrenade), nameof(FragGrenade.ServersideExplosion))]
    public class GrenadePatch
    {
        public static void Prefix(FragGrenade __instance)
        {
            var data = new BFFEventManager.GrenadeThrowData()
            {
                team = __instance.NetworkthrowerTeam,
                Userid = __instance.thrower.GetComponent<ReferenceHub>().GetUserId()
            };
            BFFEventManager.grenades = (data);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXILED;
using EXILED.Extensions;
using Harmony;

namespace BetterFriendlyFire
{
    public class BFF : Plugin
    {
        public override string getName => "BetterFriendlyFire";
        public BFFEventManager PLEV;
        public HarmonyInstance inst;
        public int maxTK = 3;
        public float maxScpDist = 30f;
        public bool useScpNear = true;
        public bool useInElevator = true;

        public override void OnDisable()
        {
            Events.PlayerHurtEvent -= PLEV.PlayerHurt;
            Events.RoundStartEvent -= PLEV.RoundStart;
            PLEV = null;
            inst.UnpatchAll();
        }

        public override void OnEnable()
        {
            maxTK = Config.GetInt("bff_max_teamkills", 3);
            maxScpDist = Config.GetFloat("bff_max_scp_dist", 30f);
            useScpNear = Config.GetBool("bff_use_scp_dist", true);
            useInElevator = Config.GetBool("bff_use_in_elevator", true);
            PLEV = new BFFEventManager(this);
            Events.PlayerHurtEvent += PLEV.PlayerHurt;
            Events.RoundStartEvent += PLEV.RoundStart;
            inst = HarmonyInstance.Create("virtualbrightplayz.exiledbetterfriendlyfire");
            inst.PatchAll();
        }

        public override void OnReload()
        {
        }
    }
}

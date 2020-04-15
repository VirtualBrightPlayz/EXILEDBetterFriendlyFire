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
        public int maxTK = 2;
        public float maxScpDist = 30f;
        public bool useScpNear = true;
        public bool useInElevator = true;
        public float TKInterval = 30f;
        public bool antiRevenge = true;
        public float RevengeTimer = 60f;
        public int maxTeamSquad = 5;
        public float maxTeamDist = 20f;
        public bool useTeam = true;
        public bool teamRadiusMulti = true;
        public int maxTeamMultiplyTimes = 3;

        public override void OnDisable()
        {
            if (!Config.GetBool("bff_enable", false))
            {
                return;
            }
            Events.PlayerHurtEvent -= PLEV.PlayerHurt;
            Events.RoundStartEvent -= PLEV.RoundStart;
            Events.PlayerSpawnEvent -= PLEV.PlayerSpawn;
            PLEV = null;
            inst.UnpatchAll();
        }

        public override void OnEnable()
        {
            if (!Config.GetBool("bff_enable", false))
            {
                return;
            }
            ReloadConfig();
            PLEV = new BFFEventManager(this);
            Events.PlayerHurtEvent += PLEV.PlayerHurt;
            Events.RoundStartEvent += PLEV.RoundStart;
            Events.PlayerSpawnEvent += PLEV.PlayerSpawn;
            inst = HarmonyInstance.Create("virtualbrightplayz.exiledbetterfriendlyfire");
            inst.PatchAll();
        }

        public void ReloadConfig()
        {
            maxTK = Config.GetInt("bff_max_teamkills", 2);
            maxScpDist = Config.GetFloat("bff_max_scp_dist", 30f);
            useScpNear = Config.GetBool("bff_use_scp_dist", true);
            useInElevator = Config.GetBool("bff_use_in_elevator", true);
            TKInterval = Config.GetFloat("bff_interval", 30f);
            antiRevenge = Config.GetBool("bff_anti_revenge", true);
            RevengeTimer = Config.GetFloat("bff_anti_revenge_cooldown", 60f);
            useTeam = Config.GetBool("bff_group_protect", true);
            maxTeamDist = Config.GetFloat("bff_group_dist", 20f);
            maxTeamSquad = Config.GetInt("bff_group_size", 5);
            teamRadiusMulti = Config.GetBool("bff_group_multiply_dist_with_group_size", true);
            maxTeamMultiplyTimes = Config.GetInt("bff_group_multiply_max_times", 3);
        }

        public override void OnReload()
        {
        }
    }
}

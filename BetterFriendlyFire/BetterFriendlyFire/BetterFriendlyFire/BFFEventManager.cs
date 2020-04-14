using System;
using System.Collections.Generic;
using EXILED;
using EXILED.Extensions;
using UnityEngine;

namespace BetterFriendlyFire
{
    public class BFFEventManager
    {
        private BFF plugin;
        internal static Dictionary<string, int> teamKills;
        internal static GrenadeThrowData grenades;
        internal static List<Lift> lifts;

        public class GrenadeThrowData
        {
            public string Userid;
            public Team team;
            public float Time;
        }

        public BFFEventManager(BFF bFF)
        {
            this.plugin = bFF;
            if (teamKills == null)
            {
                teamKills = new Dictionary<string, int>();
            }
            if (lifts == null)
                lifts = new List<Lift>();
        }

        internal void RoundStart()
        {
            lifts.Clear();
            lifts.AddRange(GameObject.FindObjectsOfType<Lift>());
        }

        internal void CheckKill(ref PlayerHurtEvent ev, Team team, Team target)
        {
            bool ntf = (target == Team.MTF || target == Team.RSC) && (team == Team.MTF || team == Team.RSC);
            bool ci = (target == Team.CHI || target == Team.CDP) && (team == Team.CHI || team == Team.CDP);
            bool scpNear = plugin.useScpNear ? IsScpNearby(ev.Player, plugin.maxScpDist) : false;
            bool inElevator = plugin.useInElevator ? IsNearElevator(ev.Player, 8f) : false;
            if ((ntf || ci) && !scpNear && !inElevator)
            {
                if (!teamKills.ContainsKey(ev.Player.GetUserId()))
                {
                    teamKills.Add(ev.Player.GetUserId(), 0);
                }
                if (teamKills[ev.Player.GetUserId()] >= plugin.maxTK)
                {
                    ev.Amount = 0f;
                    ev.Attacker.Broadcast(3, "Max teamkills reached.", false);
                    return;
                }
                else if (ev.Player.GetHealth() - ev.Amount <= 0f)
                {
                    teamKills[ev.Player.GetUserId()]++;
                    ev.Attacker.Broadcast(3, "Teamkills Left: " + (plugin.maxTK - teamKills[ev.Player.GetUserId()]), false);
                }
            }
            else if ((ntf || ci) && (inElevator || scpNear))
            {
                ev.Amount = 0f;
                ev.Attacker.Broadcast(3, "Cannot teamkill now.", false);
                return;
            }
        }

        internal bool IsNearElevator(ReferenceHub player, float v)
        {
            float dist = v;
            Lift lift = null;
            foreach (var item in lifts)
            {
                if (Vector3.Distance(item.transform.position, player.transform.position) < dist)
                {
                    dist = Vector3.Distance(item.transform.position, player.transform.position);
                    lift = item;
                }
            }
            return lift != null;
        }

        internal bool IsScpNearby(ReferenceHub player, float maxdist)
        {
            float dist = maxdist;
            ReferenceHub scp = null;
            foreach (var plr in PlayerManager.players)
            {
                if (plr.GetPlayer().GetTeam() == Team.SCP)
                {
                    if (Vector3.Distance(plr.transform.position, player.transform.position) < dist)
                    {
                        dist = Vector3.Distance(plr.transform.position, player.transform.position);
                        scp = plr.GetPlayer();
                    }
                }
            }
            return scp != null;
        }

        internal void PlayerHurt(ref PlayerHurtEvent ev)
        {
            if (ev.Attacker == null) return; // no player
            if (ev.Attacker == ev.Player) return; // same player
            //if (ev.Player.GetHealth() - ev.Amount > 0f) return;
            if (ev.DamageType == DamageTypes.Grenade)
            {
                if (grenades == null)
                {
                    ev.Amount = 0f;
                    return;
                }
                var team = grenades.team;
                var target = ev.Player.GetTeam();
                CheckKill(ref ev, team, target);
            }
            else
            {
                var team = ev.Attacker.GetTeam();
                var target = ev.Player.GetTeam();
                CheckKill(ref ev, team, target);
            }
        }
    }
}
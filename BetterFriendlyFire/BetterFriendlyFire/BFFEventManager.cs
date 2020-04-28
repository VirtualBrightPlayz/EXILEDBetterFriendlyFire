using System;
using System.Collections.Generic;
using EXILED;
using EXILED.Extensions;
using MEC;
using UnityEngine;

namespace BetterFriendlyFire
{
    public class BFFEventManager
    {
        private BFF plugin;
        internal static Dictionary<string, int> teamKills;
        internal static Dictionary<string, float> teamKillTimers;
        internal static List<TeamKillCoolDown> teamKillRevengeTimers;
        internal static GrenadeThrowData grenades;
        internal static List<Lift> lifts;
        internal static List<string> broadcasting;

        public class GrenadeThrowData
        {
            public string Userid;
            public Team team;
            public float Time;
        }

        public class TeamKillCoolDown
        {
            public string NotRevengeUserId;
            public string UserId;
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
            if (teamKillTimers == null)
            {
                teamKillTimers = new Dictionary<string, float>();
            }
            if (teamKillRevengeTimers == null)
            {
                teamKillRevengeTimers = new List<TeamKillCoolDown>();
            }
            if (broadcasting == null)
                broadcasting = new List<string>();
        }

        internal void PlayerSpawn(PlayerSpawnEvent ev)
        {
            if (ev.Player.characterClassManager.CurClass == RoleType.Spectator) return;
            ResetRevengeTimer(ev.Player);
        }

        private void ResetRevengeTimer(ReferenceHub player)
        {
            foreach (var item in teamKillRevengeTimers)
            {
                if (item.UserId.Equals(player.GetUserId()) || item.NotRevengeUserId.Equals(player.GetUserId()))
                {
                    teamKillRevengeTimers[teamKillRevengeTimers.IndexOf(item)].Time = Time.timeSinceLevelLoad;
                }
            }
        }

        internal void RoundStart()
        {
            lifts.Clear();
            lifts.AddRange(GameObject.FindObjectsOfType<Lift>());
            teamKills.Clear();
            teamKillTimers.Clear();
            teamKillRevengeTimers.Clear();
            broadcasting.Clear();
            foreach (var ply in PlayerManager.players)
            {
                teamKillTimers.Add(ply.GetPlayer().GetUserId(), -999f);
            }
        }

        internal bool IsPossibleRevenge(ref PlayerHurtEvent ev)
        {
            if (!plugin.antiRevenge)
                return false;
            TeamKillCoolDown cooldown = null;
            foreach (var item in teamKillRevengeTimers)
            {
                if (item.UserId.Equals(ev.Attacker.GetUserId()) && item.NotRevengeUserId.Equals(ev.Player.GetUserId()) && Time.timeSinceLevelLoad - item.Time <= plugin.RevengeTimer)
                {
                    cooldown = item;
                    break;
                }
            }
            if (cooldown != null)
            {
                return true;
            }
            return false;
        }

        internal bool IsTeamSquadNearby(ReferenceHub evPly, Team team, Team target, float radius, int times = 0)
        {
            
            if (!plugin.useTeam) return false;
            int count = 0;
            foreach (var ply in PlayerManager.players)
            {
                bool ntf = (ply.GetPlayer().GetTeam() == Team.MTF || ply.GetPlayer().GetTeam() == Team.RSC) && (team == Team.MTF || team == Team.RSC);
                bool ci = (ply.GetPlayer().GetTeam() == Team.CHI || ply.GetPlayer().GetTeam() == Team.CDP) && (team == Team.CHI || team == Team.CDP);
                if (!(ntf || ci)) continue;
                if (Vector3.Distance(ply.transform.position, evPly.transform.position) < radius)
                {
                    if (plugin.teamRadiusMulti && times < plugin.maxTeamMultiplyTimes)
                    {
                        return IsTeamSquadNearby(evPly, team, target, radius + plugin.maxTeamDist, times + 1);
                    }
                    else
                    {
                        count++;
                    }
                }
            }
            if (count >= plugin.maxTeamSquad)
                return true;
            return false;
        }

        internal void CheckKill(ref PlayerHurtEvent ev, Team team, Team target, ReferenceHub attacker, ReferenceHub player)
        {
            if (attacker == null || player == null)
            {
                SetAmountZero(ref ev);
                return;
            }
            bool ntf = (target == Team.MTF || target == Team.RSC || ((target == Team.CHI || target == Team.CDP) && player.IsHandCuffed())) && (team == Team.MTF || team == Team.RSC);
            bool ci = (target == Team.CHI || target == Team.CDP || ((target == Team.MTF || target == Team.RSC) && player.IsHandCuffed())) && (team == Team.CHI || team == Team.CDP);
            bool scpNear = plugin.useScpNear ? IsScpNearby(player, plugin.maxScpDist) : false;
            bool inElevator = plugin.useInElevator ? IsNearElevator(player, 20f) : false;
            if ((ntf || ci))
            {
                if (!teamKills.ContainsKey(attacker.GetUserId()))
                {
                    teamKills.Add(attacker.GetUserId(), 0);
                }
                if (!teamKillTimers.ContainsKey(attacker.GetUserId()))
                    teamKillTimers.Add(attacker.GetUserId(), 0f);
                var curtks = teamKills[attacker.GetUserId()];
                if (team == Team.RIP || Time.timeSinceLevelLoad - teamKillTimers[attacker.GetUserId()] <= plugin.TKInterval || scpNear || inElevator || IsTeamSquadNearby(player, team, target, plugin.maxTeamDist))
                {
                    SetAmountZero(ref ev);
                    BroadCastToPlayer(attacker, 3, "You cannot teamkill now.");
                    return;
                }
                else if (IsPossibleRevenge(ref ev))
                {
                    SetAmountZero(ref ev);
                    BroadCastToPlayer(attacker, 3, "You cannot revengekill.");
                    return;
                }
                else if (teamKills[attacker.GetUserId()] >= plugin.maxTK)
                {
                    SetAmountZero(ref ev);
                    BroadCastToPlayer(attacker, 3, "Max teamkills reached.");
                    return;
                }
                else if (player.GetHealth() - ev.Amount <= 0f)
                {
                    teamKills[attacker.GetUserId()]++;
                    attacker.Broadcast(3, "Teamkills Left: " + (plugin.maxTK - (curtks + 1)), false);
                    if (!teamKillTimers.ContainsKey(attacker.GetUserId()))
                        teamKillTimers.Add(attacker.GetUserId(), Time.timeSinceLevelLoad);
                    teamKillTimers[attacker.GetUserId()] = Time.timeSinceLevelLoad;
                    var data = new TeamKillCoolDown()
                    {
                        NotRevengeUserId = attacker.GetUserId(),
                        Time = Time.timeSinceLevelLoad,
                        UserId = player.GetUserId()
                    };
                    teamKillRevengeTimers.Add(data);
                }
            }
            /*else if ((ntf || ci) && (inElevator || scpNear))
            {
                ev.Amount = 0f;
                BroadCastToPlayer(ev.Attacker, 3, "You cannot teamkill now.");
                return;
            }*/
        }

        internal void SetAmountZero(ref PlayerHurtEvent ev)
        {
            ev.Amount = 0f;
            var info = ev.Info;
            info.Amount = 0f;
            ev.Info = info;
        }

        internal void BroadCastToPlayer(ReferenceHub attacker, uint v1, string v2)
        {
            if (broadcasting.Contains(attacker.GetUserId()))
                return;
            attacker.Broadcast(v1, v2, false);
            broadcasting.Add(attacker.GetUserId());
            Timing.RunCoroutine(RemoveBroadcasting(attacker));
        }

        internal IEnumerator<float> RemoveBroadcasting(ReferenceHub attacker)
        {
            yield return Timing.WaitForSeconds(5f);
            if (broadcasting.Contains(attacker.GetUserId()))
                broadcasting.Remove(attacker.GetUserId());
        }

        internal bool IsNearElevator(ReferenceHub player, float v)
        {
            //float dist = v;
            //Lift lift = null;
            foreach (var item in lifts)
            {
                foreach (var item2 in item.elevators)
                {
                    if (Vector3.Distance(item2.target.position, player.transform.position) < v)
                    {
                        //dist = Vector3.Distance(item2.target.position, player.transform.position);
                        //lift = item;
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool IsScpNearby(ReferenceHub player, float maxdist)
        {
            //float dist = maxdist;
            //ReferenceHub scp = null;
            foreach (var plr in PlayerManager.players)
            {
                if (plr.GetPlayer().GetTeam() == Team.SCP)
                {
                    if (Vector3.Distance(plr.transform.position, player.transform.position) < maxdist)
                    {
                        //dist = Vector3.Distance(plr.transform.position, player.transform.position);
                        //scp = plr.GetPlayer();
                        return true;
                    }
                }
            }
            //return scp != null;
            return false;
        }

        internal void PlayerHurt(ref PlayerHurtEvent ev)
        {
            if (ev.Attacker == null) return; // no player
            if (ev.Attacker == ev.Player) return; // same player
            //if (ev.Player.GetHealth() - ev.Amount > 0f) return;
            if ((ev.DamageType.name == DamageTypes.Grenade.name || ev.Info.GetDamageType().name == DamageTypes.Grenade.name))
            {
                if (grenades == null)
                {
                    SetAmountZero(ref ev);
                    return;
                }
                var team = grenades.team;
                var target = ev.Player.GetTeam();
                Log.Debug("Grenade Team: " + team.ToString() + " Player Team: " + target.ToString());
                CheckKill(ref ev, team, target, Player.GetPlayer(grenades.Userid), ev.Player);
            }
            else
            {
                var team = ev.Attacker.GetTeam();
                var target = ev.Player.GetTeam();
                CheckKill(ref ev, team, target, ev.Attacker, ev.Player);
            }
        }
    }
}
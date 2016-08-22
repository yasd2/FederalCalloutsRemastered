
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Threading;
using System.Diagnostics;
using System.Drawing;

//Our namespace (aka folder) where we keep our callout classes.
namespace FederalCallouts.Callouts
{
#if DEBUG
    [CalloutInfo("Assassination", CalloutProbability.Always)]
#else
    [CalloutInfo("Assassination", CalloutProbability.High)]
#endif
    public class Assassination : Callout
    {
        /*
         * TODO:
         * More advanced suspect identification (age and race)
         * Advanced killer tracking (instead of forcing a wander every 10 seconds or so)
         */
        private Ped assassin;
        private Ped victim;
        private Vector3 SpawnPoint;
        private Blip assassinAreaBlip;
        private Blip killDebugBlip;
        private Blip vicDebugBlip;
        private bool onScene = false;
        private Stopwatch sw = new Stopwatch();
        private AssassinCalloutState state = AssassinCalloutState.Following;
        //private bool killerMovingIn = false;
        //private bool killerGoodToShoot = false;
        //milliseconds after callout start for assassin to strike
        private int msToStart;
        private int msToNextHint = 15000;
        private int msToNextDistcheck = 5000;
        private LHandle pursuit;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(600f));

            //Create our ped in the world
            assassin = new Ped(SpawnPoint);
            victim = new Ped(SpawnPoint);
            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, SpawnPoint);
            assassin.Inventory.GiveNewWeapon("WEAPON_COMBATPISTOL", 250, false);
            // Set up our callout message and location
            this.CalloutMessage = "Assassination in progress";
            this.CalloutPosition = SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_SUSPICIOUS_ACTIVITY IN_OR_ON_POSITION", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            sw.Start();
            msToStart = new Random().Next(Settings.AssassinMinStrikeTime, Settings.AssassinMaxStrikeTime);
            Game.LogTrivialDebug(string.Format("[FC] The assassin will strike after {0} seconds", msToStart));
            //converting the randomly generated seconds to milliseconds
            msToStart *= 1000;
            Game.DisplaySubtitle("~w~Locate the ~r~assassin~w~ in the ~y~search area~w~ before they kill.", 6500);
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            assassinAreaBlip = new Blip(SpawnPoint, 60f);
            assassinAreaBlip.Color = Color.FromArgb(100, 255, 255, 0);
            assassinAreaBlip.EnableRoute(Color.Yellow);
            GameFiber.StartNew((() =>
            {
                Dispatch.PlayerSay("Copy that dispatch, I'm en route.");
                GameFiber.Wait(1000);
                if (assassin.IsMale)
                {
                    Dispatch.Notify("Copy, the suspected assassin is a ~r~male~w~.");
                }
                else
                {
                    Dispatch.Notify("Copy, the suspected assassin is a ~r~female~w~.");
                }
            }));

            assassin.Tasks.Wander();
            victim.Tasks.Wander();
            assassin.MakePersistent();
            victim.MakePersistent();
            assassin.RelationshipGroup = new RelationshipGroup("assassins");
            victim.RelationshipGroup = new RelationshipGroup("victims");
            //This should solve the victim attacking the player
            Game.SetRelationshipBetweenRelationshipGroups(assassin.RelationshipGroup, victim.RelationshipGroup, Relationship.Neutral);
            Game.SetRelationshipBetweenRelationshipGroups(victim.RelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Respect);
            victim.CanAttackFriendlies = false;
#if DEBUG
            killDebugBlip = assassin.AttachBlip();
            vicDebugBlip = victim.AttachBlip();
            vicDebugBlip.Color = Color.Blue;
#endif
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (assassinAreaBlip.Exists()) { assassinAreaBlip.Delete(); }
            if (vicDebugBlip.Exists()) { vicDebugBlip.Delete(); }
            if (killDebugBlip.Exists()) { killDebugBlip.Delete(); }
            if (assassin.Exists()) { assassin.Dismiss(); }
            if (victim.Exists()) { victim.Dismiss(); }
            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();
            if (sw.ElapsedMilliseconds >= msToStart && state == AssassinCalloutState.Following)
            {
                Game.LogTrivial("[FC] Assassin is moving in");
                state = AssassinCalloutState.MovingIn;

            }
            if (!onScene && Vector3.Distance(assassinAreaBlip.Position, Game.LocalPlayer.Character.Position) < 60f)
            {
                assassinAreaBlip.DisableRoute();
                onScene = true;
            }

            //Make sure the assassin doesn't get too far from victim, also make sure he keeps wandering
            if (state == AssassinCalloutState.Following && ((double)Vector3Extension.DistanceTo(assassin.Position, victim.Position) >= 40.0f))
            {
                if (sw.ElapsedMilliseconds > msToNextDistcheck)
                {
                    Game.LogTrivial("[FC]Telling assassin to get closer to victim");
                    msToNextDistcheck += (10 * 1000);
                    assassin.Tasks.FollowNavigationMeshToPosition(victim.Position.AroundPosition(5f), float.Parse(victim.Heading.ToString()), 1.5f);
                }
            }
            else
            {
                if (sw.ElapsedMilliseconds > msToNextDistcheck && state == AssassinCalloutState.Following)
                {
                    msToNextDistcheck += (15 * 1000);
                    Game.LogTrivialDebug("[FC] Telling assassin to wander");
                    assassin.Tasks.Wander();
                }
            }
            //Make the killer close distance between him and the victim before firing
            if (state == AssassinCalloutState.MovingIn && ((double)Vector3Extension.DistanceTo(assassin.Position, victim.Position) >= 20.0f))
            {
                if (sw.ElapsedMilliseconds > msToNextDistcheck)
                {
                    Game.LogTrivialDebug("[FC] Assassin closing distance before attacking victim");
                    msToNextDistcheck += (10 * 1000);
                    assassin.Tasks.FollowNavigationMeshToPosition(victim.Position, float.Parse(victim.Heading.ToString()), 1.9f);
                }
            }
            else if (state == AssassinCalloutState.MovingIn)
            {
                Game.LogTrivial("[FC] Assassin is good to shoot");
                state = AssassinCalloutState.GoodToShoot;
            }
            //Only have this if statement run once to prevent killer from being in an infinite loop
            if (state == AssassinCalloutState.GoodToShoot)
            {
                Game.LogTrivial("[FC] Assassin is opening fire");
                state = AssassinCalloutState.Attacking;
                assassin.Tasks.FightAgainst(victim);
            }
            //Update search area every 20 seconds
            if (!Functions.IsPedArrested(assassin) && assassin.IsAlive && sw.ElapsedMilliseconds > msToNextHint)
            {
                msToNextHint += (20 * 1000);
                assassinAreaBlip.Position = assassin.Position.AroundPosition(35f);
            }
            if (state == AssassinCalloutState.Attacking & !victim.IsAlive & !onScene)
            {
                state = AssassinCalloutState.Fleeing;
                if (killDebugBlip.Exists())
                    killDebugBlip.Delete();
                killDebugBlip = assassin.AttachBlip();
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, assassin);
                GameFiber.StartNew((() =>
                {
                    Dispatch.Notify("Be advised, " + Settings.PlayerName + " 911 callers report that the victim is down.");
                    GameFiber.Wait(500);
                    Dispatch.Notify("The assassin is on the run, I am sending their ~r~location~w~to you now.");
                    GameFiber.Wait(750);
                    Dispatch.PlayerSay("Copy that dispatch, pursuing suspect.");
                }));
            }
            if (Functions.IsPedArrested(assassin) || !assassin.IsAlive)
            {
                if (!assassin.IsAlive)
                    GameFiber.StartNew((() =>
                    {
                        GameFiber.Wait(1000);
                        Dispatch.PlayerSay("Dispatch, the assassin is down.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_DOWN");
                        Dispatch.Copy("suspect is down.");
                    }));
                if (Functions.IsPedArrested(assassin))
                    GameFiber.StartNew((() =>
                    {
                        Dispatch.PlayerSay("Dispatch, suspect is in custody.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_ARRESTED");
                        Dispatch.Copy("suspect is in custody.");
                    }));
                if (victim.Exists() && victim.IsAlive && Functions.IsPedArrested(victim)) { victim.Tasks.Wander(); }
                End();
            }
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (assassinAreaBlip.Exists()) { assassinAreaBlip.Delete(); }
            if (vicDebugBlip.Exists()) { vicDebugBlip.Delete(); }
            if (killDebugBlip.Exists()) { killDebugBlip.Delete(); }
            if (assassin.Exists()) { assassin.Dismiss(); }
            if (victim.Exists()) { victim.Dismiss(); }
            base.End();

        }
    }
}
public enum AssassinCalloutState
{
    Following,
    MovingIn,
    GoodToShoot,
    Attacking,
    Fleeing
}
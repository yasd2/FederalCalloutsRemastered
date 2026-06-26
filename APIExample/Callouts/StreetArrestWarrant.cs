using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using FederalCallouts.UI;
namespace FederalCallouts.Callouts
{
    //TODO: make peds wanted
#if DEBUG
    [CalloutInfo("StreetArrestWarrant", CalloutProbability.Medium)]
#else
    [CalloutInfo("StreetArrestWarrant", CalloutProbability.Medium)]
#endif
    public class StreetArrestWarrant : Callout
    {
        /*
         * TODO:
         * Fix vehicle theft
         */
        private Ped suspect;
        private PedAwarenessIndicator indicator;
        private string reason;
        private Blip suspectBlip;
        private Vector3 SpawnPoint;
        private bool suspectArmed = false;
        private LHandle pursuit;
        private string[] weapons = {
                                       "WEAPON_KNIFE",
                                       "WEAPON_HAMMER",
                                       "WEAPON_BAT",
                                       "WEAPON_CROWBAR",
                                       "WEAPON_PISTOL",
                                       "WEAPON_COMBATPISTOL",
                                       "WEAPON_APPISTOL",
                                       "WEAPON_PISTOL50",
                                       "WEAPON_MICROSMG",
                                       "WEAPON_SAWNOFFSHOTGUN",
                                       "WEAPON_STUNGUN",
                                       "WEAPON_CARBINERIFLE"
                                   };
        private StreetArrestState state = StreetArrestState.EnRoute;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(600f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            AddMinimumDistanceCheck(5f, SpawnPoint);
            suspect = new Ped(SpawnPoint);
            //prevent lspdfr crashes due to non existant ped
            if (!suspect.Exists())
                return false;

            Persona p = Functions.GetPersonaForPed(suspect);
            p.Wanted = true;
            Functions.SetPersonaForPed(suspect, p);
            
            suspect.Tasks.Wander();
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_WANTED_FELON IN_OR_ON_POSITION SUSPECT_IS ON_FOOT", suspect.Position);
            CalloutMessage = "Wanted person";
            CalloutPosition = SpawnPoint;
            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            suspectBlip = suspect.AttachBlip();
            suspectBlip.EnableRoute(Color.Yellow);
            suspectBlip.Scale = 0.75f;
            reason = Realism.GetArrestReason(out suspectArmed);
            if (suspectArmed)
            {
                Game.LogTrivialDebug("Giving suspect a weapon");
                WeaponAsset w = new WeaponAsset(weapons[new Random().Next(0, weapons.Length - 1)]);
                suspect.Inventory.GiveNewWeapon(w, 100, false);
            }
            GameFiber.StartNew((() =>
                    {
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, I'm moving in to apprehend the suspect.");
                        GameFiber.Wait(1000);
                        Game.DisplayNotification("~b~Dispatch~w~: Copy, " + Settings.PlayerName + " be advised: suspect may be armed and dangerous. Use caution.");
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY PROCEED_WITH_CAUTION");
                        GameFiber.Wait(750);
                        Persona s = Functions.GetPersonaForPed(suspect);
                        if (suspect.IsMale)
                        {
                            Game.DisplayNotification(string.Format("~b~Dispatch~w~: Suspect details:~n~Gender: ~r~Male~w~~n~Name: ~r~{0}", s.FullName));
                        }
                        else
                        {
                            Game.DisplayNotification(string.Format("~b~Dispatch~w~: Suspect details:~n~Gender: ~r~Female~w~~n~Name: ~r~{0}", s.FullName));
                        }
                        GameFiber.Wait(1000);
                        Game.DisplayNotification(string.Format("~b~Dispatch~w~: Suspect is wanted for: ~r~{0}~w~.", reason));
                        GameFiber.Wait(2000);
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Copy.");
                    }));
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            if (state == StreetArrestState.OnScene)
            {
                indicator.Think();
            }
            if (state == StreetArrestState.EnRoute)
            {
                //Done in a seperate if statement to test optimization
                if (Game.LocalPlayer.Character.DistanceTo(suspect) <= 125.0f)
                {
                    GameFiber.StartNew((() =>
                    {
                        Dispatch.PlayerSay("Dispatch, I'm on scene and moving in to make the arrest");
                        GameFiber.Wait(2000);
                        Dispatch.Copy();
                    }));
                    state = StreetArrestState.OnScene;
                    suspectBlip.DisableRoute();
                    indicator = new PedAwarenessIndicator(suspect);
                    indicator.Noticed += OnPlayerNoticed;
                }
                if (Game.LocalPlayer.Character.DistanceTo(suspect) <= 60.0f)
                {
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn)
                        HandlePlayerUsingSiren();
                }
            }
            if (!suspect.IsAlive || Functions.IsPedArrested(suspect))
            {
                if (!suspect.IsAlive)
                    GameFiber.StartNew((() =>
                    {
                        GameFiber.Wait(1000);
                        Dispatch.PlayerSay("suspect is dead");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_DOWN");
                        Dispatch.Copy("Suspect is dead");
                    }));
                if (Functions.IsPedArrested(suspect))
                    GameFiber.StartNew((() =>
                    {
                        Dispatch.PlayerSay("Dispatch, suspect is in custody");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_ARRESTED");
                        Dispatch.Copy("Suspect is in custody");
                    }));
                End();
            }
            base.Process();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            indicator.Remove();
            base.End();
        }

        void OnPlayerNoticed(EventArgs e)
        {
            int rand = new Random().Next(0, 101);
            if (rand <= 40 && suspectArmed)
            {
                Game.LogTrivial("[FC] Fugitive engaging player");
                suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
            }
            else if (false & rand >= 41 && rand <= 70)
            {
                GameFiber.StartNew((() =>
                {
                    Game.LogTrivial("[FC] Fugitive stealing a car and fleeing");
                    Vehicle[] vehicles = suspect.GetNearbyVehicles(2);
                    Vehicle v = vehicles[0];
                    //Prefer to not steal player's vehicle
                    if (v.IsPlayersVehicle() && vehicles.Length > 1)
                        v = vehicles[1];
                    if (v.Exists() & v.IsValid())
                    {
                        suspect.Tasks.FollowNavigationMeshToPosition(v.Position, v.Heading, 50000f);
                        while (suspect.Exists()
                            & suspect.IsAlive
                            & !Functions.IsPedGettingArrested(suspect)
                            & !Functions.IsPedArrested(suspect)
                            & Vector3.Distance(suspect.Position, v.Position) >= 6f)
                        {
                            GameFiber.Sleep(250);
                        }
                        GameFiber.Sleep(100);
                        //if the car is moving then the suspect will be too far again so go straight to the pursuit
                        if (Vector3.Distance(suspect.Position, v.Position) < 6f)
                        {
                            if (v.HasDriver)
                            {
                                //nothing to do yet until better way to steal cars is determined
                                goto Skip;
                            }
                            else
                                suspect.Tasks.EnterVehicle(v, -1);
                            while (suspect.IsAlive
                                & !Functions.IsPedGettingArrested(suspect)
                                & !Functions.IsPedArrested(suspect)
                                & !suspect.IsInAnyVehicle(false))
                            {
                                //Hold the fucking phone until they're in a car
                                GameFiber.Wait(100);
                            }
                        }
                    Skip:
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                    }
                    else
                        Game.LogTrivialDebug("[FC] No vehicles detected :^)");
                }));
            }
            else if (rand >= 71 && rand <= 80)
            {
                Game.LogTrivial("[FC] Fugitive surrendering");
                suspect.Tasks.PutHandsUp(1000, Game.LocalPlayer.Character);
            }
            else
            {
                Game.LogTrivial("[FC] Fugitive fleeing");
                //create pursuit
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, suspect);
            }
            state = StreetArrestState.SuspectResisting;
        }

        private void HandlePlayerUsingSiren()
        {
            state = StreetArrestState.SuspectResisting;
            if (new Random().Next(0, 101) <= 71 && suspectArmed)
            {
                Game.LogTrivial("[FC] Player siren spooked armed suspect, engaging player");
                suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
            }
            else
            {
                Game.LogTrivial("[FC] Player siren spooked suspect, creating pursuit");
                //create pursuit
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, suspect);
            }
        }
    }
}
public enum StreetArrestState
{
    EnRoute,
    OnScene,
    SuspectResisting
}
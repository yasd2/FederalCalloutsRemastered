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
#if DEBUG
    [CalloutInfo("PrisonerEscaped", CalloutProbability.Medium)]
#else
    [CalloutInfo("PrisonerEscaped", CalloutProbability.High)]
#endif
    /*
     * TODO:
     * 
     * 
     * Backlog:
     * Add peds variations
     * Control variations so we know what race to give descriptions to the player
     * Make the player catch the prisoner committing crimes
     */
    class PrisonerEscaped : Callout
    {
        private float searchDist = 90.0F;
        private Ped fugitive;
        private Blip fugitiveBlip;
        private Blip fugitiveSearchBlip;
        private PedAwarenessIndicator indicator;
        private Vector3 SpawnPoint;
        private string reason;
        private bool fugitiveArmed = false;
        private LHandle pursuit;
        private int nextUpdate = 0;
        private string fugitiveDescription;
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
                                       "WEAPON_STUNGUN"
                                   };
        private string[] peds = { "s_m_y_prisoner_01", "u_m_y_prisoner_01" };
        private PrisonEscapedState state = PrisonEscapedState.EnRoute;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(450f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            AddMinimumDistanceCheck(5f, SpawnPoint);
            fugitive = new Ped("s_m_y_prisoner_01", SpawnPoint, 0);
            if (!fugitive.Exists())
                return false;
            fugitiveDescription = "an orange jumpsuit";
            fugitive.Tasks.Wander();
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_WANTED_FELON IN_OR_ON_POSITION SUSPECT_IS ON_FOOT", fugitive.Position);
            CalloutMessage = "Escaped prisoner";
            CalloutPosition = SpawnPoint;

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
#if DEBUG
            fugitiveBlip = fugitive.AttachBlip();
#endif
            reason = Realism.GetArrestReason(out fugitiveArmed);
            if (fugitiveArmed)
            {
                Game.LogTrivialDebug("Giving fugitive a weapon");
                WeaponAsset w = new WeaponAsset(weapons[new Random().Next(0, weapons.Length - 1)]);
                fugitive.Inventory.GiveNewWeapon(w, 100, false);
            }
            fugitiveSearchBlip = new Blip(fugitive.Position.AroundPosition(searchDist - 10f), searchDist + 5F);
            fugitiveSearchBlip.Color = Color.FromArgb(100, 255, 255, 0);
            fugitiveSearchBlip.EnableRoute(Color.Yellow);
            GameFiber.StartNew((() =>
            {
                Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Copy that dispatch, en route to apprehend.");
                GameFiber.Wait(1000);
                Game.DisplayNotification("~b~Dispatch~w~: Copy, " + Settings.PlayerName + " be advised: fugitive may be armed and dangerous. Use caution.");
                Functions.PlayScannerAudio("REPORT_RESPONSE_COPY PROCEED_WITH_CAUTION");
                GameFiber.Wait(750);
                Game.DisplayNotification("~b~Dispatch~w~: Stand by for information.");
                GameFiber.Wait(15000);
                Persona s = Functions.GetPersonaForPed(fugitive);
                Game.DisplayNotification(string.Format("~b~Dispatch~w~: The fugitive is a male by the name of ~g~{0}", s.FullName));
                GameFiber.Wait(750);
                Game.DisplayNotification(string.Format("~b~Dispatch~w~: He's wearing {0}", fugitiveDescription));
                GameFiber.Wait(1000);
                Game.DisplayNotification(string.Format("~b~Dispatch~w~: The suspect was in jail for ~r~{0}~w~.", reason));
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
            if (fugitive.Exists()) { fugitive.Dismiss(); }
            if (fugitiveBlip.Exists()) { fugitiveBlip.Delete(); }
            if (fugitiveSearchBlip.Exists()) { fugitiveSearchBlip.Delete(); }
            base.OnCalloutNotAccepted();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            if (state == PrisonEscapedState.OnScene)
            {
                indicator.Think();
            }
            if (state == PrisonEscapedState.EnRoute)
            {
                //Done in a seperate if statement to test optimization
                if ((double)Vector3Extension.DistanceTo(Game.LocalPlayer.Character.Position, fugitiveSearchBlip.Position) <= searchDist)
                {
                    GameFiber.StartNew((() =>
                    {
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, I'm on scene and looking for the fugitive.");
                        GameFiber.Wait(2000);
                        Game.DisplayNotification("~b~Dispatch~w~: Copy.");
                    }));
                    state = PrisonEscapedState.OnScene;
                    indicator = new PedAwarenessIndicator(fugitive);
                    indicator.Noticed += OnPlayerNoticed;
                    fugitiveSearchBlip.DisableRoute();
                }
                if ((double)Vector3Extension.DistanceTo(Game.LocalPlayer.Character.Position, fugitive.Position) <= 50.0f)
                {
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn)
                        HandlePlayerUsingSiren();
                }
            }
            if ((state == PrisonEscapedState.EnRoute || state == PrisonEscapedState.OnScene) && (int)Game.GameTime >= nextUpdate)
            {
                GameFiber.StartNew((() =>
                {
                    Game.DisplayNotification("~b~Dispatch~w~: We've received a tip on where the fugitive is.");
                }));
                nextUpdate = (int)Game.GameTime + new Random().Next(20 * 1000, 45 * 1000);
                fugitiveSearchBlip.Position = fugitive.Position.AroundPosition(searchDist - 10F);
            }
            if (!fugitive.IsAlive || Functions.IsPedArrested(fugitive))
            {
                if (!fugitive.IsAlive)
                    GameFiber.StartNew((() =>
                    {
                        GameFiber.Wait(1000);
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, the fugitive is dead.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_DOWN");
                        Game.DisplayNotification("~b~Dispatch~w~: Copy, fugitive is dead.");
                    }));
                if (Functions.IsPedArrested(fugitive))
                    GameFiber.StartNew((() =>
                    {
                        Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Dispatch, the fugitive is back  in custody.");
                        GameFiber.Wait(2000);
                        Functions.PlayScannerAudio("REPORT_RESPONSE_COPY SUSPECT_ARRESTED");
                        Game.DisplayNotification("~b~Dispatch~w~: Copy, fugitive is in custody.");
                    }));
                End();
            }
            base.Process();
        }

        private void OnPlayerNoticed(EventArgs e)
        {
            Game.LogTrivial("[FC] Callout is in action phase");
            if (!fugitiveBlip.Exists())
            {
                fugitiveBlip = fugitive.AttachBlip();
                fugitiveBlip.Scale = 0.75f;
            }

            int rand = new Random().Next(0, 101);

            Game.LogTrivial(string.Format("[FC] Dice roll was {0}", rand));
            if (rand <= 40 && fugitiveArmed)
            {
                Game.LogTrivial("[FC] Fugitive engaging player");
                fugitive.Tasks.FightAgainst(Game.LocalPlayer.Character);
            }
            else if (false && rand >= 41 && rand <= 70)
            {
                //TODO: Put something here
                //
                //
                //
                //
            }
            else if (rand >= 71 && rand <= 80)
            {
                Game.LogTrivial("[FC] Fugitive surrendering");
                fugitive.Tasks.PutHandsUp(10000, Game.LocalPlayer.Character);
            }
            else
            {
                Game.LogTrivial("[FC] Fugitive fleeing");
                //create pursuit
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, fugitive);
            }
            state = PrisonEscapedState.Action;
            if (fugitiveSearchBlip.Exists())
                fugitiveSearchBlip.Delete();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (fugitive.Exists()) { fugitive.Dismiss(); }
            if (fugitiveBlip.Exists()) { fugitiveBlip.Delete(); }
            if (fugitiveSearchBlip.Exists()) { fugitiveSearchBlip.Delete(); }
            indicator.Remove();
            base.End();
        }



        private void HandlePlayerUsingSiren()
        {
            state = PrisonEscapedState.Action;
            if (fugitiveSearchBlip.Exists())
                fugitiveSearchBlip.Delete();
            if (fugitiveArmed)
            {
                Game.LogTrivial("[FC] Player siren spooked armed fugitive, engaging player");
                fugitive.Tasks.FightAgainst(Game.LocalPlayer.Character);
            }
            else
            {
                Game.LogTrivial("[FC] Player siren spooked fugitive, creating pursuit");
                //create pursuit
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, fugitive);
            }
        }
    }
    public enum PrisonEscapedState
    {
        EnRoute,
        OnScene,
        Action
    }
}

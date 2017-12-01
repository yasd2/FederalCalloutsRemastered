using LSPD_First_Response;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using FederalCallouts.Tools;
//Our namespace (aka folder) where we keep our callout classes.
namespace FederalCallouts.Callouts
{

    /*
     * TODO:
     * 
     */
#if DEBUG
    [CalloutInfo("ArmoredCarRobbery", CalloutProbability.Always)]
#else
    [CalloutInfo("ArmoredCarRobbery", CalloutProbability.Medium)]
#endif
    public class ArmoredCarRobbery : Callout
    {
        private Ped guard1, guard2, attacker1, attacker2, attacker3, attacker4;
        private Blip gBlip1, gBlip2, aBlip1, aBlip2, aBlip3, aBlip4, vBlip;
        private Vehicle securicar, robberyVan;
        private Vector3 SpawnPoint;
        private bool robbersEnRoute = true;
        private uint lastLocationUpdate = 0;
        private CarRobberyState state = CarRobberyState.EnRoute;
        private bool swatDispatched = false;
        private bool playerPromptedToSwitch = false;
        private bool playerSwitchedChars = false;
        private bool playerPromptedToEnd = false;
        private Vehicle swatVehicle;
        private CharacterManager charMan = new CharacterManager();
        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            RelationshipGroup r = new RelationshipGroup("ROBBERS");

            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(500f));
            GameFiber.StartNew((() =>
            {
                //Vector3 robberSpawn = World.GetNextPositionOnStreet(SpawnPoint.Around(95f));
                Vector3 robberSpawn = Extensions.FindCloseSpawn(SpawnPoint, 95f, 130f);
                robberyVan = new Vehicle("burrito3", robberSpawn);
                attacker1 = new Ped(robberSpawn);
                attacker2 = new Ped(robberSpawn);
                attacker3 = new Ped(robberSpawn);
                attacker4 = new Ped(robberSpawn);
                attacker1.RelationshipGroup = r;
                attacker2.RelationshipGroup = r;
                attacker3.RelationshipGroup = r;
                attacker4.RelationshipGroup = r;
                attacker1.Armor = 100;
                attacker2.Armor = 100;
                attacker3.Armor = 100;
                attacker4.Armor = 100;
                attacker1.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                attacker2.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PUMPSHOTGUN"), 500, true);
                attacker3.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_SMG"), 500, true);
                attacker4.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                attacker1.WarpIntoVehicle(robberyVan, (int)robberyVan.GetFreeSeatIndex());
                attacker2.WarpIntoVehicle(robberyVan, (int)robberyVan.GetFreeSeatIndex());
                attacker3.WarpIntoVehicle(robberyVan, (int)robberyVan.GetFreeSeatIndex());
                attacker4.WarpIntoVehicle(robberyVan, (int)robberyVan.GetFreeSeatIndex());
                attacker1.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                robberyVan.IsPersistent = true;
                robberyVan.IsInvincible = true;

            }), "FC Armored Car Robbery Init");
            guard1 = new Ped("s_m_m_armoured_01", SpawnPoint, 0.0f);
            guard2 = new Ped("s_m_m_armoured_02", SpawnPoint, 0.0f);
            GameFiber.Sleep(6);
            guard1.CanAttackFriendlies = false;
            guard2.CanAttackFriendlies = false;
            guard1.Armor = 100;
            guard2.Armor = 100;
            RelationshipGroup g = new RelationshipGroup("SECURITY");
            Game.LogTrivialDebug(g.Name);
            guard1.RelationshipGroup = "SECURITY";
            guard2.RelationshipGroup = g;
            Game.SetRelationshipBetweenRelationshipGroups(g, "COP", Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups("COP", g, Relationship.Like);
            Game.SetRelationshipBetweenRelationshipGroups(g, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Companion);
            Game.SetRelationshipBetweenRelationshipGroups(Game.LocalPlayer.Character.RelationshipGroup, g, Relationship.Like);
            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", g, Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups(g, r, Relationship.Dislike);
            guard1.Inventory.GiveNewWeapon("WEAPON_COMBATPISTOL", 250, true);
            guard2.Inventory.GiveNewWeapon("WEAPON_COMBATPISTOL", 250, false);
            guard2.Inventory.GiveNewWeapon("WEAPON_SMG", 500, true);
            securicar = new Vehicle("stockade", SpawnPoint);
            guard1.WarpIntoVehicle(securicar, (int)securicar.GetFreeSeatIndex());
            guard2.WarpIntoVehicle(securicar, (int)securicar.GetFreeSeatIndex());
            guard1.Tasks.CruiseWithVehicle(securicar, 9.0f, VehicleDrivingFlags.RespectIntersections | VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.AvoidHighways);
            //Driving styles: 
            //786468 -Slow, ignores lights still, ignores cars
            //262144 looks the same as the first
            //786469
            Rage.Native.NativeFunction.CallByName<uint>("SET_DRIVE_TASK_DRIVING_STYLE", guard1, 786603);
            Game.LogTrivialDebug(string.Format("g1|{0}|{1}", guard1.RelationshipGroup, guard2.RelationshipGroup.Name));
            Game.LogTrivialDebug(string.Format("g2|{0}|{1}", guard2.RelationshipGroup, guard2.RelationshipGroup.Name));
            Game.LogTrivialDebug(string.Format("p0|{0}|{1}", Game.LocalPlayer.Character.RelationshipGroup, Game.LocalPlayer.Character.RelationshipGroup.Name));
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, SpawnPoint);
            securicar.IsPersistent = true;


            this.CalloutMessage = "Armored car robbery";
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_ARMORED_CAR_ROBBERY IN_OR_ON_POSITION", securicar.Position);
            this.CalloutPosition = SpawnPoint;

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            GameFiber.StartNew((() =>
            {
                GameFiber.Wait(1000);
                Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Copy, I'm en route.");
                GameFiber.Wait(1000);
                Dispatch.Notify("Copy, proceed with caution.");
                Functions.PlayScannerAudio("REPORT_RESPONSE_COPY PROCEED_WITH_CAUTION");
            }));
            vBlip = robberyVan.AttachBlip();
            attacker1.Tasks.DriveToPosition(securicar.Position, 70.0f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects);
            gBlip1 = guard1.AttachBlip();
            gBlip1.Color = Color.LightBlue;

            gBlip1.EnableRoute(Color.Yellow);
            gBlip2 = guard2.AttachBlip();
            gBlip2.Color = Color.LightBlue;
            gBlip1.Scale = 0.75f;
            gBlip2.Scale = 0.75f;
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (guard1.Exists()) { guard1.Dismiss(); }
            if (guard2.Exists()) { guard2.Dismiss(); }
            if (robberyVan.Exists()) { robberyVan.Dismiss(); }
            if (securicar.Exists()) { securicar.Dismiss(); }
            if (gBlip1.Exists()) { gBlip1.Delete(); }
            if (gBlip2.Exists()) { gBlip2.Delete(); }
            if (aBlip1.Exists()) { aBlip1.Delete(); }
            if (aBlip2.Exists()) { aBlip2.Delete(); }
            if (aBlip3.Exists()) { aBlip3.Delete(); }
            if (aBlip4.Exists()) { aBlip4.Delete(); }
            if (vBlip.Exists()) { vBlip.Delete(); }
            if (attacker1.Exists()) { attacker1.Dismiss(); }
            if (attacker2.Exists()) { attacker2.Dismiss(); }
            if (attacker3.Exists()) { attacker3.Dismiss(); }
            if (attacker4.Exists()) { attacker4.Dismiss(); }

            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {

            if (robbersEnRoute && (double)Vector3Extension.DistanceTo(securicar.Position, robberyVan.Position) <= 29.0f)
            {
                robbersEnRoute = false;
                if (state == CarRobberyState.EnRoute)
                    GameFiber.StartNew((() =>
                    {
                        if (vBlip.Exists())
                            vBlip.Delete();
                        Dispatch.Notify("Be advised: Guards have confirmed that the robbers are on scene.");
                        GameFiber.Wait(1500);
                        Game.LogTrivialDebug(Game.LocalPlayer.Character.Model.Name);
                        if (Game.LocalPlayer.Character.Model.Name.ToLower() == "mp_m_fibsec_01" ||
                            Game.LocalPlayer.Character.Model.Name == "0x5cdef405" ||
                            Game.LocalPlayer.Character.Model.Name == "0x7b8b434b")
                        {
                            Dispatch.Notify("Dispatching ~g~FIB SWAT~w~ to assist.");
                            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_SWAT_UNITS ASSISTANCE_REQUIRED IN_OR_ON_POSITION", securicar.Position);
                            //IF YOU CHANGE THIS CHANGE THE OTHER ONE
                            Vehicle swatGranger = Functions.RequestBackup(securicar.Position, EBackupResponseType.Code3, EBackupUnitType.NooseTeam);
                            //swatGranger.Position = Extensions.FindCloseSpawn(securicar.Position, 150f, 175f);
                            //Gathered from http://ragepluginhook.net/PedModels.aspx?modelHash=2374966032 , this will make them have FIB SWAT textures
                            GameFiber.Wait(2000);
                            foreach (Ped p in swatGranger.Passengers)
                            {
                                if (p.Exists())
                                    p.SetVariation(10, 0, 1);
                            }
                            if (swatGranger.Driver.Exists())
                                swatGranger.Driver.SetVariation(10, 0, 1);
                            //Making the guards and the SWAT friendly with each other
                            vBlip = swatGranger.AttachBlip();
                            swatVehicle = swatGranger;
                            vBlip.Color = Color.LightBlue;
                            Game.SetRelationshipBetweenRelationshipGroups(swatGranger.Driver.RelationshipGroup, guard1.RelationshipGroup, Relationship.Companion);
                            Game.SetRelationshipBetweenRelationshipGroups(guard1.RelationshipGroup, swatGranger.Driver.RelationshipGroup, Relationship.Companion);
                        }
                        else
                        {
                            Dispatch.Notify("Dispatching a  ~g~SWAT~w~ unit to assist.");
                            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_SWAT_UNITS ASSISTANCE_REQUIRED IN_OR_ON_POSITION", securicar.Position);
                            //IF YOU CHANGE THIS CHANGE THE OTHER ONE
                            Vehicle swatGranger = Functions.RequestBackup(securicar.Position, EBackupResponseType.Code3, EBackupUnitType.NooseTeam);
                            //swatGranger.Position = Extensions.FindCloseSpawn(securicar.Position, 150f, 175f);
                            GameFiber.Wait(1000);
                            swatVehicle = swatGranger;
                            vBlip = swatGranger.AttachBlip();
                            vBlip.Color = Color.LightBlue;
                            Game.SetRelationshipBetweenRelationshipGroups(swatGranger.Driver.RelationshipGroup, guard1.RelationshipGroup, Relationship.Companion);
                            Game.SetRelationshipBetweenRelationshipGroups(guard1.RelationshipGroup, swatGranger.Driver.RelationshipGroup, Relationship.Companion);
                        }
                        GameFiber.StartNew((() =>
                        {
                            GameFiber.Sleep(1500);
                            swatVehicle.MakePersistent();
                            if (swatVehicle.PassengerCount == 0)
                            {
                                Game.LogTrivial("[FC] Recreating vehicle crew");
                                for (int i = 0; i < 3; i++)
                                {
                                    Ped p = new Ped("s_m_y_swat_01", swatVehicle.Position.Around(10), 0);
                                    for (int h = 0; h < 50; h++)
                                    {
                                        if (p)
                                            break;
                                        else
                                            GameFiber.Wait(5);
                                    }
                                    if (p)
                                    {
                                        Functions.SetPedAsCop(p);
                                        p.Armor = 100;
                                        p.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_CARBINERIFLE"), 500, true);
                                        p.WarpIntoVehicle(swatVehicle, swatVehicle.GetFreePassengerSeatIndex() ?? i);
                                        p.Tasks.Clear();
                                    }
                                }
                            }
                            if (!swatVehicle.Driver)
                            {
                                Game.LogTrivial("[FC] Recreating vehicle driver");
                                Ped p = new Ped("s_m_y_swat_01", swatVehicle.Position.Around(10), 0);
                                for (int h = 0; h < 50; h++)
                                {
                                    if (p)
                                        break;
                                    else
                                        GameFiber.Wait(5);
                                }
                                if (p)
                                {
                                    Functions.SetPedAsCop(p);
                                    p.Armor = 100;
                                    p.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_CARBINERIFLE"), 500, true);
                                    p.WarpIntoVehicle(swatVehicle, swatVehicle.GetFreeSeatIndex() ?? -1);
                                    p.Tasks.DriveToPosition(securicar.Position, 100, VehicleDrivingFlags.Emergency);
                                }
                            }
                            swatDispatched = true;
                        }), "FC Fiber 2");
                    }), "FC Armored Car Robbery Backup");

                Game.LogTrivialDebug("[FC] Attaching attacker blips");
                aBlip1 = attacker1.AttachBlip();
                aBlip2 = attacker2.AttachBlip();
                aBlip3 = attacker3.AttachBlip();
                aBlip4 = attacker4.AttachBlip();
                aBlip1.Scale = 0.75f;
                aBlip2.Scale = 0.75f;
                aBlip3.Scale = 0.75f;
                aBlip4.Scale = 0.75f;
                GameFiber.StartNew(((() =>
                {
                    Game.LogTrivialDebug("[FC] Attackers are getting out of their vehicle");
                    if (attacker1.Exists() && attacker1.IsInAnyVehicle(false)) { attacker1.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                    if (attacker2.Exists() && attacker2.IsInAnyVehicle(false)) { attacker2.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                    if (attacker3.Exists() && attacker3.IsInAnyVehicle(false)) { attacker3.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                    if (attacker4.Exists() && attacker4.IsInAnyVehicle(false)) { attacker4.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                    Game.LogTrivialDebug("[FC] They got out successfully :^)");
                    GameFiber.Sleep(1500);
                    Game.LogTrivialDebug("[FC] Attackers attacking");
                    attacker1.Tasks.FightAgainstClosestHatedTarget(100f);
                    attacker2.Tasks.FightAgainstClosestHatedTarget(100f);
                    attacker3.Tasks.FightAgainstClosestHatedTarget(100f);
                    attacker4.Tasks.FightAgainstClosestHatedTarget(100f);
                    GameFiber.Sleep(2000);
                    Game.LogTrivialDebug("[FC] Blowing off the rear doors");
                    if (securicar.Exists())
                    {
                        VehicleDoor r1 = securicar.Doors[2];
                        VehicleDoor r2 = securicar.Doors[3];
                        r1.BreakOff();
                        r2.BreakOff();
                    }
                    Game.LogTrivialDebug("[FC] Guards are exiting the vehicle");
                    if (guard1.Exists() && guard1.IsInAnyVehicle(false)) { guard1.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                    if (guard2.Exists() && guard2.IsInAnyVehicle(false)) { guard2.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                    GameFiber.Sleep(1500);
                    guard1.Tasks.FightAgainstClosestHatedTarget(50f);
                    guard2.Tasks.FightAgainstClosestHatedTarget(50f);
                })));
            }
            if (robbersEnRoute && Game.GameTime > lastLocationUpdate + 5000)
            {
                attacker1.Tasks.DriveToPosition(securicar.Position, 70f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects);
                lastLocationUpdate = Game.GameTime;
            }
            if (state == CarRobberyState.EnRoute && (double)Vector3Extension.DistanceTo(Game.LocalPlayer.Character.Position, securicar.Position) <= 60.0f)
            {
                state = CarRobberyState.OnScene;
                gBlip1.DisableRoute();
                GameFiber.StartNew((() =>
                {
                    Dispatch.PlayerSay("Dispatch, I've located the armored car.");
                    GameFiber.Wait(2000);
                    Dispatch.Copy();
                }));
            }
            if (state == CarRobberyState.EnRoute & swatDispatched)
            {
                if (!playerPromptedToSwitch)
                {
                    Game.DisplayHelp("Press ~g~" + Settings.StartKey.ToString() + "~w~ now to switch to the SWAT team.");
                    playerPromptedToSwitch = true;
                }
                if (Game.IsKeyDown(Settings.StartKey))
                {
                    if (!swatVehicle.Driver.Exists())
                    {
                        Game.DisplayNotification("SWAT driver was cleaned up by GTA, could not switch.");
                    }
                    else
                    {
                        playerSwitchedChars = true;
                        GameFiber.StartNew((() =>
                        {
                            if (swatVehicle.PassengerCount > 0)
                                foreach (Ped p in swatVehicle.Passengers)
                                    p.Delete();
                            charMan.SwitchToPed(swatVehicle.Driver);
                            GameFiber.Sleep(4000);
                            Game.LogTrivial("[FC] Recreating vehicle crew");
                            for (int i = 0; i < 3; i++)
                            {
                                Ped p = new Ped("s_m_y_swat_01", swatVehicle.Position.Around(10), 0);
                                for (int h = 0; h < 50; h++)
                                {
                                    if (p)
                                        break;
                                    else
                                        GameFiber.Wait(5);
                                }
                                if (p)
                                {
                                    Functions.SetPedAsCop(p);
                                    p.Armor = 100;
                                    p.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_CARBINERIFLE"), 500, true);
                                    p.WarpIntoVehicle(swatVehicle, swatVehicle.GetFreePassengerSeatIndex() ?? i);
                                    p.Tasks.Clear();
                                }
                            }
                        }), "Unnamed FC Fiber");
                    }
                }
            }
            bool c1, c2, c3, c4;
            if (attacker1) { c1 = true; } else { c1 = false; }
            if (attacker2) { c2 = true; } else { c2 = false; }
            if (attacker3) { c3 = true; } else { c3 = false; }
            if (attacker4) { c4 = true; } else { c4 = false; }
            if (c1 && Functions.IsPedArrested(attacker1) || !attacker1.IsAlive) { c1 = false; }
            if (c2 && Functions.IsPedArrested(attacker2) || !attacker2.IsAlive) { c2 = false; }
            if (c3 && Functions.IsPedArrested(attacker3) || !attacker3.IsAlive) { c3 = false; }
            if (c4 && Functions.IsPedArrested(attacker4) || !attacker4.IsAlive) { c4 = false; }
            //False means they are dead, arrested or do not exist so it is safe to end the callout
            if (!c1 && !c2 && !c3 && !c4)
            {
                if (playerSwitchedChars)
                {
                    if (!playerPromptedToEnd)
                    {
                        Game.DisplayHelp("Press ~g~" + Settings.StartKey + "~w~ to end the callout and switch to your character.");
                        Game.DisplaySubtitle("Press ~g~" + Settings.StartKey + "~w~ to end the callout and switch to your character.", 30);
                        playerPromptedToEnd = true;
                    }
                }
                if(!playerSwitchedChars | Game.IsKeyDown(Settings.StartKey))
                {
                    GameFiber.StartNew((() =>
                    {
                        GameFiber.Wait(1000);
                        Dispatch.PlayerSay("Dispatch, the situation is ~g~code 4~w~.");
                        GameFiber.Wait(2000);
                        Dispatch.Copy();
                    }));
                    End();
                }
            }
            base.Process();
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (guard1) { guard1.Dismiss(); }
            if (guard2) { guard2.Dismiss(); }
            if (robberyVan) { robberyVan.Dismiss(); }
            if (securicar) { securicar.Dismiss(); }
            if (gBlip1) { gBlip1.Delete(); }
            if (gBlip2) { gBlip2.Delete(); }
            if (aBlip1) { aBlip1.Delete(); }
            if (aBlip2) { aBlip2.Delete(); }
            if (aBlip3) { aBlip3.Delete(); }
            if (aBlip4) { aBlip4.Delete(); }
            if (vBlip) { vBlip.Delete(); }
            if (attacker1) { attacker1.Dismiss(); }
            if (attacker2) { attacker2.Dismiss(); }
            if (attacker3) { attacker3.Dismiss(); }
            if (attacker4) { attacker4.Dismiss(); }
            if (playerSwitchedChars)
                charMan.SwitchToBackup();
            base.End();
        }
    }
}
public enum CarRobberyState
{
    EnRoute,
    OnScene,
    Clear
}
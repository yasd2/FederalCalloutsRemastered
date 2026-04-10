using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Threading;
using System.Drawing;
using FederalCallouts.UI;
/*
 * Things to add:
 * Callout ends if player is in marked car
 * More ending alternatives
 * Make the cartel backup go to the seller's current position
 * 
 * TODO:
 * Make the yellow warning blip show at beginning of callout
 * 
 */
//Our namespace (aka folder) where we keep our callout classes. 
namespace FederalCallouts.Callouts
{
    //Give your callout a string name and a probability of spawning. We also inherit from the Callout class, as this is a callout
    //[CalloutInfo("PotentialDrugDeal", CalloutProbability.Medium)]
#if DEBUG
    [CalloutInfo("PotentialDrugDeal", CalloutProbability.Medium)]
#else
    [CalloutInfo("PotentialDrugDeal", CalloutProbability.Medium)]
#endif
    public class PotentialDrugDeal : Callout
    {
        private Ped cartel1, cartel2, cartel3, cartel4, seller, buyer;
        Vehicle vic;
        PedAwarenessIndicator indicator;
        private DrugCalloutState state;
        private Blip sellerBlip;
        private Blip vicBlip;
        private bool spooked = false;
        private float heading = 0f;
        private bool customSpawn = false;
        private Vector3 SpawnPoint;
        private LHandle pursuit;
        Vector3 backupPos;
        private Blip noticeBlip;
        private bool cartelBackupEnRoute = false;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
#if DEBUG
            SpawnPoint = Extensions.GetDrugDealLocation(Game.LocalPlayer.Character.Position, 300f, out heading);
#else
            SpawnPoint = Extensions.GetDrugDealLocation(Game.LocalPlayer.Character.Position, 600f, out heading);
#endif

            //Create our ped in the world
            seller = new Ped("a_m_y_mexthug_01", SpawnPoint, 0f);
            //buyer = new Ped(SpawnPoint.Around(15f));
            if (heading != 0.0F)
            {
                seller.Heading = heading;
                customSpawn = true;
            }
            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            //if (!buyer.Exists()) return false;
            if (!seller.Exists()) return false;
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            AddMinimumDistanceCheck(5f, seller.Position);
            seller.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL50"), 500, false);
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_DRUG_DEAL IN_OR_ON_POSITION", SpawnPoint);
            this.CalloutMessage = "Potential drug deal";
            this.CalloutPosition = SpawnPoint;
            return base.OnBeforeCalloutDisplayed();
        }
        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            noticeBlip = new Blip(seller.Position, 21.0f);
            noticeBlip.Color = Color.FromArgb(100, 255, 255, 0);
            indicator = new PedAwarenessIndicator(seller);
            indicator.Noticed += OnPlayerNoticed;
            GameFiber.StartNew((() =>
            {
                Game.DisplayNotification("~b~" + Settings.PlayerName + "~w~: Copy dispatch, I'm moving in to provide surveillance.");
                GameFiber.Wait(1500);
                Game.DisplayNotification("~b~Dispatch~w~: Copy, do not apprehend the dealer until you see him take the money.");
                GameFiber.Wait(500);
                Game.DisplayNotification("~b~Dispatch~w~: We need all the evidence we can get on this guy.");
                GameFiber.Wait(15 * 1000);
                Game.DisplayHelp("Press ~g~" + Settings.StartKey.ToString() + "~w~ to begin surveillance", 45);
            }));
            Game.DisplaySubtitle("~w~Surveil the potential ~r~seller~w~. Do not get too close. Press ~g~Y~w~ to begin surveillance.", 6500);
            GameFiber.StartNew(() =>
            {
                GameFiber.Wait(45 * 1000);
                if (state == DrugCalloutState.EnRoute)
                    Game.DisplayHelp("Press ~g~" + Settings.StartKey.ToString() + "~w~ at any time to move in on suspect.");
            });
            sellerBlip = seller.AttachBlip();
            sellerBlip.Color = Color.Yellow;
            sellerBlip.EnableRoute(Color.Yellow);
            sellerBlip.Scale = 0.75f;

            //We only want him to wander if we do not have a custom spawn
            //This is because we know custom spawns are safe whereas random spawns may put him on street
            if (!customSpawn)
                GameFiber.StartNew((() =>
                {
                    seller.Tasks.Wander();
                    GameFiber.Sleep(25000);
                    seller.Tasks.StandStill(30000);
                }));

            return base.OnCalloutAccepted();
        }

        private void OnPlayerNoticed(EventArgs e)
        {
            Game.DisplaySubtitle("~w~The ~r~seller~w~ noticed you.", 6500);
            spooked = true;
            End();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            if (seller.Exists()) { seller.Dismiss(); }
            if (buyer.Exists()) { buyer.Dismiss(); }
            if (sellerBlip.Exists()) { sellerBlip.Delete(); }
            base.OnCalloutNotAccepted();
        }
        public override void Process()
        {
            base.Process();
            if (state != DrugCalloutState.Interception & state != DrugCalloutState.DealHappened & Game.LocalPlayer.Character.DistanceTo(seller) < 200f)
                indicator.Think();
            if (state == DrugCalloutState.EnRoute && Game.IsKeyDown(Settings.StartKey))
            {
                buyer = new Ped(Extensions.FindCloseSpawn(seller.Position, 60f, 60f));
                sellerBlip.DisableRoute();
#if DEBUG
                buyer.AttachBlip().Color = Color.Blue;
#endif
                state = DrugCalloutState.Surveillance;
                StartDeal();
            }
            if (noticeBlip.Exists())
                noticeBlip.Position = seller.Position;

            if(state == DrugCalloutState.Surveillance)
            {
                if(buyer.Exists() && !buyer.IsAlive)
                {
                    Game.DisplaySubtitle("~w~The  ~g~buyer~w~ has been killed.", 6500);
                    End();
                }
            }

            if (state == DrugCalloutState.DealHappened)
            {
                if (((double)Vector3Extension.DistanceTo(Game.LocalPlayer.Character.Position, seller.Position) <= 25.0f))
                {
                    state = DrugCalloutState.Interception;
                    HandlePlayerMovingIn();
                }
            }
            
            if (cartelBackupEnRoute && vic.Exists() && cartel1.Exists())
            {
                if ((double)Vector3Extension.DistanceTo(backupPos, vic.Position) <= 50.0f)
                {
                    Game.LogTrivial("[FC] Cartel backup arrived");
                    cartelBackupEnRoute = false;
                    GameFiber.StartNew((() =>
                    {

                        if (cartel1.Exists()) { cartel1.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                        if (cartel2.Exists()) { cartel2.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                        if (cartel3.Exists()) { cartel3.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                        if (cartel4.Exists() && cartel4.IsInAnyVehicle(false)) { cartel4.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen); }
                        Game.DisplaySubtitle("~r~The dealer must have called in for backup!", 6500);
                        GameFiber.Sleep(1000);
                        if (cartel2.Exists()) { cartel1.Tasks.FightAgainst(Game.LocalPlayer.Character); }
                        if (cartel2.Exists()) { cartel2.Tasks.FightAgainst(Game.LocalPlayer.Character); }
                        if (cartel3.Exists()) { cartel3.Tasks.FightAgainst(Game.LocalPlayer.Character); }
                        if (cartel4.Exists()) { cartel4.Tasks.FightAgainst(Game.LocalPlayer.Character); }
                    }));
                }
            }

            if (Functions.IsPedArrested(seller) || !seller.IsAlive)
            {
                //If no backup is en route end the callout
                if (!cartelBackupEnRoute)
                    End();
                bool c1, c2, c3, c4;
                if (cartel1.Exists()) { c1 = true; } else { c1 = false; }
                if (cartel2.Exists()) { c2 = true; } else { c2 = false; }
                if (cartel3.Exists()) { c3 = true; } else { c3 = false; }
                if (cartel4.Exists()) { c4 = true; } else { c4 = false; }
                if (c1 && Functions.IsPedArrested(cartel1) || !cartel1.IsAlive) { c1 = false; }
                if (c2 && Functions.IsPedArrested(cartel2) || !cartel2.IsAlive) { c2 = false; }
                if (c3 && Functions.IsPedArrested(cartel3) || !cartel3.IsAlive) { c3 = false; }
                if (c4 && Functions.IsPedArrested(cartel4) || !cartel4.IsAlive) { c4 = false; }
                //False means they are dead, arrested or do not exist so it is safe to end the callout
                if (!c1 && !c2 && !c3 && !c4)
                    End();
            }
        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (seller.Exists()) { seller.Dismiss(); }
            if (buyer.Exists()) { buyer.Dismiss(); }
            if (sellerBlip.Exists()) { sellerBlip.Delete(); }
            if (noticeBlip.Exists()) { noticeBlip.Delete(); }
            if (vicBlip.Exists()) { vicBlip.Delete(); }
            if (vic.Exists()) { vic.Dismiss(); }
            if (cartel1.Exists()) { cartel1.Dismiss(); }
            if (cartel2.Exists()) { cartel2.Dismiss(); }
            if (cartel3.Exists()) { cartel3.Dismiss(); }
            if (cartel4.Exists()) { cartel4.Dismiss(); }
            if(indicator != null)
                indicator.Remove();
            base.End();
        }

        void StartDeal()
        {
            Game.DisplaySubtitle("~w~Watch the  ~r~seller~w~. Any buyer does not need to be apprehended.", 6500);
            GameFiber.StartNew((() =>
            {
                if (!buyer.Exists() && !seller.Exists())
                {
                    Game.DisplaySubtitle("~w~Buyer or seller do not exist - ending callout", 6500);
                    End();
                }
                int path = new Random().Next(1, 11);
                seller.Tasks.StandStill(15);
                buyer.Tasks.FollowNavigationMeshToPosition(seller.Position.Around(0.6f), 0, 1.1f);
                int distChecks = 0;
                while (Vector3.Distance(seller.Position, buyer.Position) > 1f && distChecks < 100)
                {
                    GameFiber.Sleep(500);
                    distChecks++;
                }
                Rage.Native.NativeFunction.CallByName<uint>("TASK_TURN_PED_TO_FACE_COORD", buyer, seller.Position.X, seller.Position.Y, seller.Position.Z, 0);
                Rage.Native.NativeFunction.CallByName<uint>("TASK_TURN_PED_TO_FACE_COORD", seller, buyer.Position.X, buyer.Position.Y, buyer.Position.Z, 0);
                GameFiber.Sleep(750);
                if (path > 3)
                {
                    Game.LogTrivial("[FC] PATH 1: Deal in progress!");
                    //AI::TASK_CHAT_TO_PED to have them talk maybe
                    seller.Tasks.PlayAnimation(new AnimationDictionary("mp_common"), "givetake1_a", 1f, AnimationFlags.None);
                    buyer.Tasks.PlayAnimation(new AnimationDictionary("mp_common"), "givetake1_b", 1f, AnimationFlags.None);
                    GameFiber.Sleep(1000);
                    buyer.Tasks.Wander();
                    buyer.Dismiss();
                    if (!spooked)
                    {
                        Game.DisplaySubtitle("~g~Move in to apprehend the seller.", 6500);
                        if (sellerBlip.IsValid())
                            sellerBlip.Color = Color.Red;
                        state = DrugCalloutState.DealHappened;
                    }
                    if (noticeBlip.Exists()) { noticeBlip.Delete(); }
                }
                else if (path == 1)
                {
                    buyer.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL50"), 500, true);
                    buyer.Tasks.FightAgainst(buyer);
                    Game.DisplaySubtitle("~g~Move in to apprehend them both!", 6500);
                    state = DrugCalloutState.DealHappened;
                }
                else if (path == 2)
                {
                    GameFiber.StartNew((() =>
                    {
                        state = DrugCalloutState.DealHappened;
                        seller.Tasks.AimWeaponAt(buyer, 15);
                        GameFiber.Wait(15 * 1000);
                        seller.Tasks.FightAgainst(buyer);
                    }));
                }
                else
                {
                    Game.LogTrivial("[FC] PATH 2: No deal");
                    Game.LogTrivial("[FC] Making them chat...");
                    Rage.Native.NativeFunction.CallByName<uint>("TASK_CHAT_TO_PED", buyer, seller, 16, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                    GameFiber.Sleep(10000);
                    if (!spooked)
                        Game.DisplaySubtitle("~R~Our intel must have been bad, this wasn't a drug deal.", 6500);
                    seller.Tasks.Wander();
                    buyer.Tasks.Wander();
                    End();
                }
            }));
        }
        void HandlePlayerMovingIn()
        {
            GameFiber.StartNew((() =>
            {
                Game.LogTrivialDebug("[FC] Handling player moving in on seller");
                int end = new Random().Next(1, 5);
#if DEBUG
                end = 2;
#endif
                //Pretend seller does not notice player
                if (end == 1)
                {
                    Game.LogTrivialDebug("[FC] Seller did not notice player");
                    seller.Tasks.Wander();
                }
                //Seller pretends to not notice player, calls in for backup to take player out
                if (end == 2)
                {
                    Game.LogTrivial("[FC] Seller is calling in backup");
                    Rage.Native.NativeFunction.CallByName<uint>("TASK_USE_MOBILE_PHONE", seller, 1, 1);
                    //Vector3 hostileSpawn = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(175f));
                    Vector3 hostileSpawn = Extensions.FindCloseSpawn(Game.LocalPlayer.Character.Position, 160f, 190f);
                    cartel1 = new Ped("g_m_y_mexgoon_01", hostileSpawn, 0f);
                    cartel2 = new Ped("g_m_y_mexgoon_01", hostileSpawn, 0f);
                    cartel3 = new Ped("g_m_y_mexgoon_02", hostileSpawn, 0f);
                    cartel4 = new Ped("g_m_y_mexgoon_03", hostileSpawn, 0f);
                    cartel1.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                    cartel2.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                    cartel3.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                    cartel4.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_ASSAULTRIFLE"), 500, true);
                    vic = new Vehicle("burrito3", hostileSpawn);
#if DEBUG
                    vicBlip = vic.AttachBlip();
#endif
                    vic.IsInvincible = true;
                    cartel1.WarpIntoVehicle(vic, (int)vic.GetFreeSeatIndex());
                    cartel2.WarpIntoVehicle(vic, (int)vic.GetFreeSeatIndex());
                    cartel3.WarpIntoVehicle(vic, (int)vic.GetFreeSeatIndex());
                    cartel4.WarpIntoVehicle(vic, (int)vic.GetFreeSeatIndex());
                    RelationshipGroup cartel = new RelationshipGroup("cartel");
                    //make these guys hate the player
                    Game.SetRelationshipBetweenRelationshipGroups(cartel, Game.LocalPlayer.Character.RelationshipGroup, Relationship.Dislike);
                    seller.RelationshipGroup = cartel;
                    cartel1.RelationshipGroup = cartel;
                    cartel2.RelationshipGroup = cartel;
                    cartel3.RelationshipGroup = cartel;
                    cartel4.RelationshipGroup = cartel;
                    if (!cartel4.IsInVehicle(vic, true))
                    {
                        cartel4.Delete();
                    }
                    backupPos = seller.Position;
                    cartel1.Tasks.DriveToPosition(seller.Position, 75f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects);
                    cartelBackupEnRoute = true;
                    GameFiber.Sleep(5 * 1000);
                    //Phone call does not automatically finish so we have to do so ourselves :^)
                    seller.Tasks.Clear();
                    seller.Tasks.Wander();
                }
                //Seller violently resists arrest
                if (end == 3)
                {
                    Game.LogTrivialDebug("[FC] Seller is attacking player");
                    //not sure if seller will automatically pull out his gun
                    seller.Tasks.FightAgainst(Game.LocalPlayer.Character);
                }
                //Seller passively resists arrest
                if (end == 4)
                {
                    Game.LogTrivialDebug("[FC] Seller is running from player");
                    //create pursuit
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, seller);
                }
            }));
        }
    }

}
public enum DrugCalloutState
{
    EnRoute,
    Surveillance,
    DealHappened,
    Interception
}
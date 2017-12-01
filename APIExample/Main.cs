using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response;
using LSPD_First_Response.Mod.Callouts;
using FederalCallouts.Callouts;
using FederalCallouts.Tools;
using System.Windows.Forms;

//prop_fib_badge
//animation: fbi_5b_mcs_1-0
//antimation: prop_fib_badge-0
namespace FederalCallouts
{
    using LSPD_First_Response.Mod.API;
    using Rage;
    /*
     * Planned callouts:
     * Potential Bombing
     * Isolated terrorist attack
     * Bomb sting operation (suspect has fake bombs)
     * Bank robbery investigation
     * Homicide investigation
     * Execute arrest & search warrants (gang leaders, cartel leaders, robbery suspects)
     * Tail suspect to find a hideout
     * Prisoner escaped
     * Prisoner hitchhiking
     * Surveillance duty
     * Investigate corruption
     * Prisoner transport
     * 
     * 
     * TODO
     * Option to increase or decrease callout probability
     * 
     * Backlog:
     * Fix spawning (Partially done)
     * Better crash fix for drug dealer calling backup (backup despawning and invalid taskinvoker)
     * More reasons for arrest
     * 
     * 
     * 
     * 
     * PATHFIND::GET_SAFE_COORD_FOR_PED
     * 
     */
    /// <summary>
    /// Do not rename! Attributes or inheritance based plugins will follow when the API is more in depth.
    /// </summary>
    public class Main : Plugin
    {
        static uint nextRepairTime = 0;
        static List<Blip> repairBlips = new List<Blip>();
        static Vector3 repairLoc = new Vector3(0, 0, 0);
        /// <summary>
        /// Constructor for the main class, same as the class, do not rename.
        /// </summary>
        public Main()
        {

        }

        /// <summary>
        /// Called when the plugin ends or is terminated to cleanup
        /// </summary>
        public override void Finally()
        {

        }

        /// <summary>
        /// Called when the plugin is first loaded by LSPDFR
        /// </summary>
        public override void Initialize()
        {
            Game.LogTrivial(string.Format("[FC] Initializing Federal Callouts {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            //Event handler for detecting if the player goes on duty
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            InitializationFile ini = new InitializationFile("Plugins/LSPDFR/FederalCallouts.ini");
            ini.Create();
            KeysConverter kc = new KeysConverter();
            Settings.StartKey = (Keys)kc.ConvertFromString(ini.ReadString("Main", "StartKey", "Y"));
            Settings.EnableRepairModule = ini.ReadBoolean("Main", "EnableRepairs", true);
            //Enabling or disabling callouts
            Settings.EnableArmoredCarRobbery = ini.ReadBoolean("ArmoredCarRobbery", "Enable", true);
            Settings.Prob_ACR = ini.ReadInt32("ArmoredCarRobbery", "Probability", 2);

            Settings.EnablePotentialDrugDeal = ini.ReadBoolean("PotentialDrugDeal", "Enable", true);
            Settings.Prob_PDD = ini.ReadInt32("PotentialDrugDeal", "Probability", 3);

            Settings.EnableExecuteArrestWarrant = ini.ReadBoolean("ExecuteArrestWarrant", "Enable", true);
            Settings.Prob_SAW = ini.ReadInt32("ExecuteArrestWarrant", "Probability", 3);

            Settings.EnableAssassination = ini.ReadBoolean("Assassination", "Enable", true);
            Settings.Prob_Ass = ini.ReadInt32("Assassination", "Probability", 2);

            Settings.EnablePrisonerEscaped = ini.ReadBoolean("PrisonerEscaped", "Enable", true);
            Settings.Prob_PE = ini.ReadInt32("PrisonerEscaped", "Probability", 2);

            Settings.EnableKidnapping = ini.ReadBoolean("Kidnapping", "Enable", true);
            Settings.Prob_Kidn = ini.ReadInt32("Kidnapping", "Probability", 2);

            Settings.EnableBombSting = ini.ReadBoolean("BombSting", "Enable", true);
            Settings.Prob_BS = ini.ReadInt32("BombSting", "Probability", 2);

            Settings.EnableORC = ini.ReadBoolean("OrganizedRetailCrime", "Enable", true);

            Settings.EnableStingray = ini.ReadBoolean("Stingray", "Enable", true);
            Settings.Prob_Stingray = ini.ReadInt32("Stingray", "Probability", 2);
            Game.LogTrivial("[FC] Loading spawn positions");

            //Callout specific settings
            string path = Environment.CurrentDirectory + @"\Plugins\LSPDFR\FederalCallouts\Drug";
            Settings.DrugDealSpawns = new List<VectorHeading>();
            try
            {
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    string[] split = file.Split('/', '\\');
                    string f = split[split.Length - 1];
                    //string f = f.Substring()
                    foreach (var esp in Serialization.LoadFromXML<ExtendedSpawnPoint>(f))
                    {
                        VectorHeading vh = new VectorHeading();
                        vh.Position = esp.Position;
                        vh.Heading = esp.Heading;
                        Settings.DrugDealSpawns.Add(vh);
                    }
                    Game.LogTrivial("[FC] Loaded drug deal spawn file: " + f);
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[FC] Caught an exception: " + ex.Message);
            }
            path = Environment.CurrentDirectory + @"\Plugins\LSPDFR\FederalCallouts\HVT";
            Settings.ImportantBuildingSpawns = new List<VectorHeadingTag>();
            Serialization.sPath = @".\Plugins\LSPDFR\FederalCallouts\HVT\";
            try
            {
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    string[] split = file.Split('/', '\\');
                    string f = split[split.Length - 1];
                    //string f = f.Substring()
                    foreach (var esp in Serialization.LoadFromXML<ExtendedSpawnPoint>(f))
                    {
                        VectorHeadingTag vh = new VectorHeadingTag();
                        vh.Position = esp.Position;
                        vh.Heading = esp.Heading;
                        vh.Tag = esp.Tags[0];
                        Settings.ImportantBuildingSpawns.Add(vh);
                    }
                    Game.LogTrivial("[FC] Loaded HVT spawn file: " + f);
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[FC] Caught an exception: " + ex.Message);
            }
            path = Environment.CurrentDirectory + @"\Plugins\LSPDFR\FederalCallouts\ORC";
            Settings.ORCTargets = new List<Vector3>();
            Serialization.sPath = @".\Plugins\LSPDFR\FederalCallouts\ORC\";
            try
            {
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    string[] split = file.Split('/', '\\');
                    string f = split[split.Length - 1];
                    //string f = f.Substring()
                    foreach (var esp in Serialization.LoadFromXML<ExtendedSpawnPoint>(f))
                    {
                        Settings.ORCTargets.Add(esp.Position);
                    }
                    Game.LogTrivial("[FC] Loaded ORC spawn file: " + f);
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[FC] Caught an exception: " + ex.Message);
            }
            //Assassination
            //Incremented up by 1 because math.Random.Next() max value is exclusive
            Settings.AssassinMaxStrikeTime = (ini.ReadInt32("Assassination", "MaxStrikeTime", 300) + 1);
            Settings.AssassinMinStrikeTime = ini.ReadInt32("Assassination", "MinStrikeTime", 60);
            Settings.KidnappingMinimumPercent = ini.ReadInt32("Kidnapping", "PercentAccurate", 25);
#if DEBUG
            Game.LogTrivial(string.Format("[FC] Federal callouts v{0} initialized! [DEBUG ASSEMBLY]", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            //Game.LogTrivial(Environment.CurrentDirectory);
#else
            Game.LogTrivial(string.Format("[FC] Federal callouts v{0} initialized!", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
#endif
        }
        static void StartRepairModule()
        {
            GameFiber.StartNew(() =>
            {
                while (true)
                {
                    bool doRepair = false;
                    foreach (Blip b in repairBlips)
                    {
                        b.Delete();
                    }
                    repairBlips.Clear();
                    foreach (var item in Repair.Locations)
                    {
                        if (item.Key.DistanceTo(Game.LocalPlayer.Character.Position) < 2000F)
                        {
                            Blip repairBlip = new Blip(item.Key);
                            repairBlip.Sprite = BlipSprite.Repair;
                            repairBlips.Add(repairBlip);
                        }
                    }
                    //Game.LogTrivialDebug("[FC] 185");
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        if (Game.LocalPlayer.Character.CurrentVehicle.Speed < 1F)
                            foreach (var item in Repair.Locations)
                            {
                                if (item.Key.DistanceTo(Game.LocalPlayer.Character.Position) < 7F)
                                {
                                    repairLoc = item.Key;
                                    doRepair = true;
                                }
                            }
                    //Game.LogTrivialDebug("[FC] 195");
                    if (doRepair & Game.GameTime > nextRepairTime)
                    {
                        nextRepairTime = Game.GameTime + 60 * 1000;
                        GameFiber.StartNew((() =>
                        {
                            Game.DisplaySubtitle("Please wait while your car gets repaired.", 10 * 1000);
                            GameFiber.Wait(1000);
                            Game.FadeScreenOut(1000, true);
                            Vehicle v = Game.LocalPlayer.Character.CurrentVehicle;
                            v.LockStatus = VehicleLockStatus.Locked;
                            v.Repair();
                            v.Wash();
                            v.Position = Repair.Locations[repairLoc].Position;
                            v.Heading = Repair.Locations[repairLoc].Heading;
                            GameFiber.Sleep(3 * 1000);
                            Game.FadeScreenIn(1000, true);
                            Game.LocalPlayer.Character.CurrentVehicle.LockStatus = VehicleLockStatus.Unlocked;
                            Game.DisplaySubtitle("All done! ~r~(You will be unable to repair again for 1 minute)", 10 * 1000);

                        }));
                        doRepair = false;
                    }
                    GameFiber.Sleep(333);
                }
            });
        }
        /// <summary>
        /// The event handler mentioned above
        /// </summary>
        static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                if (Settings.EnablePotentialDrugDeal)
                    for(int i = 0; i <= Settings.Prob_PDD;i++)
                    Functions.RegisterCallout(typeof(PotentialDrugDeal));

                if (Settings.EnableAssassination)
                    for (int i = 0; i <= Settings.Prob_Ass; i++)
                        Functions.RegisterCallout(typeof(Assassination))
                            ;
                if (Settings.EnableExecuteArrestWarrant)
                    for (int i = 0; i <= Settings.Prob_SAW; i++)
                        Functions.RegisterCallout(typeof(StreetArrestWarrant));

                if (Settings.EnableArmoredCarRobbery)
                    for (int i = 0; i <= Settings.Prob_ACR; i++)
                        Functions.RegisterCallout(typeof(ArmoredCarRobbery));

                if (Settings.EnablePrisonerEscaped)
                    for (int i = 0; i <= Settings.Prob_PE; i++)
                        Functions.RegisterCallout(typeof(PrisonerEscaped));

                if (Settings.EnableKidnapping)
                    for (int i = 0; i <= Settings.Prob_Kidn; i++)
                        Functions.RegisterCallout(typeof(Kidnapping));

                if (Settings.EnableBombSting)
                    for (int i = 0; i <= Settings.Prob_BS; i++)
                        Functions.RegisterCallout(typeof(BombSting));

                if (Settings.EnableStingray)
                    for (int i = 0; i <= Settings.Prob_Stingray; i++)
                        Functions.RegisterCallout(typeof(Stingray));
                if (Settings.EnableRepairModule)
                    StartRepairModule();
                InitializationFile ini = new InitializationFile("Plugins/LSPDFR/FederalCallouts.ini");
                ini.Create();
                Game.LogTrivialDebug("Player model: " + Game.LocalPlayer.Model.Name);
                if (Game.LocalPlayer.Character.IsUniformed())
                    Game.LogTrivialDebug("Player is uniformed");
                /*
                 * s_m_y_hwaycop_01
                 * s_m_y_sheriff_01
                 * s_f_y_sheriff_01
                 * s_m_y_cop_01
                 * s_f_y_cop_01
                 * s_f_y_ranger_01
                 * s_m_y_ranger_01
                 */
                if (Game.LocalPlayer.Model.Name.ToLower() == "s_m_y_cop_01" ||
                    Game.LocalPlayer.Model.Name.ToLower() == "s_f_y_cop_01" ||
                    Game.LocalPlayer.Model.Name.ToLower() == "s_m_y_hwaycop_01" ||
                    Game.LocalPlayer.Model.Name.ToLower() == "s_m_y_sheriff_01" ||
                    Game.LocalPlayer.Model.Name.ToLower() == "s_f_y_sheriff_01" ||
                    Game.LocalPlayer.Model.Name.ToLower() == "0x15f8700d" ||//city female cop
                    Game.LocalPlayer.Model.Name.ToLower() == "0x4161d042") //county female cop
                {
                    Settings.PlayerName = ini.Read("Player", "LocalName", "Officer");
                }
                else
                {
                    Settings.PlayerName = ini.Read("Player", "FederalName", "Agent");
                }
            }
        }
    }
}

/*
            GameFiber.StartNew((() =>
            {
            }), "Unnamed FC Fiber");

*/

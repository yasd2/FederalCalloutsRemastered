using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FederalCallouts
{
    class Settings
    {
        /// <summary>
        /// Player name for use in dispatch radio
        /// </summary>
        public static string PlayerName;
        public static bool EnableRepairModule;
        public static bool EnableArmoredCarRobbery;
        public static bool EnablePotentialDrugDeal;
        public static bool EnableExecuteArrestWarrant;
        public static bool EnableAssassination;
        public static bool EnablePrisonerEscaped;
        public static bool EnableKidnapping;
        public static bool EnableBombSting;
        public static bool EnableStingray;
        public static bool EnableORC;
        public static Keys StartKey;
        public static int AssassinMaxStrikeTime;
        public static int AssassinMinStrikeTime;

        public static int KidnappingMinimumPercent = 25;

        public static List<VectorHeading> DrugDealSpawns;
        public static List<VectorHeadingTag> ImportantBuildingSpawns;
        public static List<Vector3> ORCTargets;
        //TODO: Add riot and van models
        public static string[] MarkedModels = { "police1", "police2", "police3", "sheriff1", "sheriff2", "policeb" };
        //TODO: Add SWAT/ASU/Etc models
        public static string[] UniformedModels = { "s_m_y_cop_01", "s_f_y_cop_01", "s_m_y_hwaycop_01",
            "s_m_y_sheriff_01", "s_f_y_sheriff_01", "s_f_y_ranger_01", "s_m_y_ranger_01", "s_m_y_swat_01" };
    }
    public struct VectorHeading
    {
        public Vector3 Position;
        public float Heading;
    }
    public struct VectorHeadingTag
    {
        public Vector3 Position;
        public float Heading;
        public string Tag;
    }
}

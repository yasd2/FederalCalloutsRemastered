using Rage;
using System;

namespace FederalCallouts
{
    static class Realism
    {
        public static string GetArrestReason(out bool armed)
        {
            string[] peaceful = { "tax fraud", "embezzlement", "computer hacking", "forgery", "identity theft","money laundering",
                                    "bankrupty fraud", "bribery", "insurance fraud", "credit card fraud", "prostitution",
                                "wire fraud", "perjury", "public corruption", "tampering with evidence"};

            string[] low = { "robbery", "burglary of a dwelling","burglary of a structure", "burglary of a conveyance", "battery", "contempt of court",
                               "domestic violence", "intimidation", "stalking",
                               "reckless endangerment", "disturbing the peace","driving while intoxicated","extortion",
                           "harassment", "public intoxication", "sexual assault", "shoplifting", "tax evasion", "vandalism",
                           "espionage", "false personation", "hooliganism", "indecent exposure", "drug manufacturing and cultivation",
                           "voluntary manslaughter", "hit and run", "assault", "attempted battery", "felony battery", "3rd degree murder",
                           "hit and run"};

            string[] medium = {"arson", "conspiracy to committ murder", "menacing", "aggravated assault", "kidnapping", "civil disorder", "racketeering",
                              "2nd degree murder", "fleeing to elude a law enforcement officer", "impersonating a police officer", "harassing a witness",
                              "resisting officer with violence", "aggravated battery", "aggravated battery on a pregnant person", "human trafficking",
                              "grand theft auto", "possession of a firearm by convicted felon", "posession of cocaine", "drug trafficking"};
            string[] high = { "armed robbery", "rape", "1st degree murder", "terrorism", "armed bank robbery", "resisting officer with violence",
                            "armored truck robbery"};
            Random r = new Random();
            int danger = r.Next(0, 101);
            if (danger < 25)
            {
                armed = false;
                return peaceful[r.Next(0, peaceful.Length - 1)];
            }
            else if (danger < 50)
            {
                if (r.Next(0, 11) < 3)
                    armed = true;
                else
                    armed = false;
                return low[r.Next(0, low.Length - 1)];
            }
            else if (danger < 85)
            {
                if (r.Next(0, 11) < 6)
                    armed = true;
                else
                    armed = false;
                return medium[r.Next(0, medium.Length - 1)];
            }
            else
            {
                if (r.Next(0, 11) < 9)
                    armed = true;
                else
                    armed = false;
                return high[r.Next(0, high.Length - 1)];
            }
        }
        public static bool IsMarked(this Vehicle v)
        {
            if (v == null | !v.IsValid() | !v.Exists()| !v.IsPoliceVehicle)
                return false;
            foreach (string s in Settings.MarkedModels)
                if (s == v.Model.Name.ToLower())
                    return true;
            return false;
        }
        public static bool IsUniformed(this Ped p)
        {
            foreach (string s in Settings.UniformedModels)
                if (s == p.Model.Name.ToLower())
                    return true;
            return false;
        }
    }
}

using OWML.Common;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CheeseTools.Utils {
    public static class RelativeBody {
        public static HashSet<OWRigidbody> GetAllBodies() {
            HashSet<OWRigidbody> bodies = new HashSet<OWRigidbody>();
            foreach (Sector sector in GameObject.FindObjectsOfType<Sector>()) {
                // if statement copied from SecterDetector.GetPassiveReferenceFrame() so only referenceable bodies are returned
                if (sector.GetName() != Sector.Name.Unnamed && sector.GetName() != Sector.Name.Ship && sector.GetName() != Sector.Name.Sun && sector.GetName() != Sector.Name.HourglassTwins) {
                    bodies.Add(sector.GetAttachedOWRigidbody());
                }
            }
            return bodies;
        }

        public static OWRigidbody GetCurrent() {
            return Locator.GetPlayerSectorDetector()?.GetPassiveReferenceFrame()?.GetOWRigidBody();
        }

        public static OWRigidbody GetFromString(string str) {
            foreach (OWRigidbody body in GetAllBodies()) {
                if (GetName(body) == str) return body;
            }
            return null;
        }

        public static string GetName(OWRigidbody body) {
            switch(body.name) {
                case "TimberHearth_Body":           return "Timber Hearth";
                case "Moon_Body":                   return "The Attlerock";
                case "BrittleHollow_Body":          return "Brittle Hollow";
                case "VolcanicMoon_Body":           return "Hollow's Lantern";
                case "GiantsDeep_Body":             return "Giant's Deep";
                case "OrbitalProbeCannon_Body":     return "Orbital Probe Cannon";
                case "TowerTwin_Body":              return "Ash Twin";
                case "CaveTwin_Body":               return "Ember Twin";
                case "DarkBramble_Body":            return "Dark Bramble";
                case "DB_HubDimension_Body":        return "DB_HubDimension";
                case "DB_VesselDimension_Body":     return "DB_VesselDimension";
                case "DB_ExitOnlyDimension_Body":   return "DB_ExitOnlyDimension";
                case "DB_AnglerNestDimension_Body": return "DB_AnglerNestDimension";
                case "DB_Elsinore_Body":            return "DB_Elsinore";
                case "DB_ClusterDimension_Body":    return "DB_ClusterDimension";
                case "DB_PioneerDimension_Body":    return "DB_PioneerDimension";
                case "DB_EscapePodDimension_Body":  return "DB_EscapePodDimension";
                case "DB_SmallNest_Body":           return "DB_SmallNest";
                case "Comet_Body":                  return "The Interloper";
                case "SunStation_Body":             return "Sun Station";
                case "QuantumMoon_Body":            return "Quantum Moon";
                case "WhiteHole_Body":              return "White Hole";
                case "EyeOfTheUniverse_Body":       return "Eye Of The Universe";
                case "RingWorld_Body":              return "The Stranger";
                case "DreamWorld_Body":             return "The Dreamworld";
            }
            return "NULL";
        }

        public static void PrintRelativeLocation(string prefix, OWRigidbody body, RelativeLocationData location) {
            CheeseTools.Console.WriteLine($"{prefix}" +
            $"Local Position: {location.localPosition.ToString("F2")}" +
            $"\nLocal Rotation: {location.localRotation.eulerAngles.ToString("F2")}" +
            $"\nBody: {GetName(body)}", MessageType.Info);
        }

        public static void PrintAllBodyNames() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("All body names:\n");
            foreach (OWRigidbody body in GetAllBodies()) {
                stringBuilder.AppendLine(GetName(body));
            }
            CheeseTools.Console.WriteLine(stringBuilder.ToString());
        }
    }
}

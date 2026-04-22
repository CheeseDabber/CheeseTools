using OWML;
using OWML.Common;
using System;
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
			if (body.TryGetComponent(out AstroObject astroObject) && astroObject.GetAstroObjectName() != AstroObject.Name.CustomString && astroObject.GetAstroObjectType() != AstroObject.Type.None) {
				return AstroObject.AstroObjectNameToString(astroObject.GetAstroObjectName());
			}
			return body.name.Replace("_Body", "");
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

	/* All printed out body names from RelativeBody.GetName():
     * Timber Hearth
     * The Attlerock
     * Brittle Hollow
     * Hollow's Lantern
     * Giant's Deep
     * Orbital Probe Cannon
     * Ash Twin
     * Ember Twin
     * Dark Bramble
     * DB_HubDimension
     * DB_VesselDimension
     * DB_ExitOnlyDimension
     * DB_AnglerNestDimension
     * DB_Elsinore
     * DB_ClusterDimension
     * DB_PioneerDimension
     * DB_EscapePodDimension
     * DB_SmallNest
     * The Interloper
     * SunStation
     * Quantum Moon
     * WhiteHole
     * The Stranger
     * DreamWorld
	 * EyeOfTheUniverse
	*/
}

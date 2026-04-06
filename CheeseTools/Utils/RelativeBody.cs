using OWML;
using OWML.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTools.Utils {
	public static class RelativeBody {
		public static List<OWRigidbody> GetAllBodies() {
			List<OWRigidbody> bodies = new List<OWRigidbody>();
			foreach (Sector sector in GameObject.FindObjectsOfType<Sector>()) {
				// if statement copied from SecterDetector.GetPassiveReferenceFrame() so only referenceable bodies are returned
				if (sector.GetName() != Sector.Name.Unnamed && sector.GetName() != Sector.Name.Ship && sector.GetName() != Sector.Name.Sun && sector.GetName() != Sector.Name.HourglassTwins) {
					bodies.SafeAdd<OWRigidbody>(sector.GetAttachedOWRigidbody());
				}
			}
			return bodies;
		}

		public static OWRigidbody GetCurrent() {
			return Locator.GetPlayerSectorDetector()?.GetPassiveReferenceFrame()?.GetOWRigidBody();
		}

		public static OWRigidbody GetFromString(string str) {
			foreach (OWRigidbody body in GetAllBodies()) {
				if (ToString(body) == str) return body;
			}
			return null;
		}

		public static string ToString(OWRigidbody body) {
			if (body.TryGetComponent<AstroObject>(out AstroObject astroObject) && astroObject.GetAstroObjectName() != AstroObject.Name.CustomString && astroObject.GetAstroObjectType() != AstroObject.Type.None) {
				return AstroObject.AstroObjectNameToString(astroObject.GetAstroObjectName());
			}
			return body.name.Replace("_Body", "");
		}

		public static void PrintRelativeLocation(string prefix, OWRigidbody body, RelativeLocationData location) {
			CheeseTools.Console.WriteLine($"{prefix}" +
			$"Local Position: {location.localPosition.ToString("F2")}" +
			$"\nLocal Rotation: {location.localRotation.eulerAngles.ToString("F2")}" +
			$"\nBody: {RelativeBody.ToString(body)}", MessageType.Success);
		}
	}
}

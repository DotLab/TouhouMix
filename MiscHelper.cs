using System;
using System.Security.Cryptography;

namespace TouhouMix {
	public static class MiscHelper {
		public static string GetBase64EncodedSha256Hash(byte[] bytes) {
			return Convert.ToBase64String(SHA256.Create().ComputeHash(bytes));
		}

		public static string GetHexEncodedMd5Hash(byte[] bytes) {
			return BitConverter.ToString(MD5.Create().ComputeHash(bytes)).Replace("-", string.Empty).ToLower();
		}
	}
}


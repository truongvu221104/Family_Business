// Helpers/PasswordHelper.cs
using System;
using System.Security.Cryptography;

namespace Family_Business.Helpers
{
    public static class PasswordHelper
    {
        public static byte[] GenerateSalt(int size = 16)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[size];
            rng.GetBytes(salt);
            return salt;
        }

        public static string Hash(string password, byte[] salt, int iterations = 10000, int length = 32)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(length);
            return Convert.ToBase64String(hashBytes);
        }
    }
}

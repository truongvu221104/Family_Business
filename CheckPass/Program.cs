using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

class Program
{
    static void Main()
    {
        string password = "Nhanvien123";        // <-- mật khẩu bạn muốn seed
        byte[] salt = GenerateSalt();
        string saltBase64 = Convert.ToBase64String(salt);
        string hashBase64 = Hash(password, salt);

        Console.WriteLine("Salt (Base64):   " + saltBase64);
        Console.WriteLine("Hash (Base64):   " + hashBase64);
    }

    static byte[] GenerateSalt()
    {
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    static string Hash(string pw, byte[] salt)
    {
        var hashed = KeyDerivation.Pbkdf2(
            password: pw,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 32
        );
        return Convert.ToBase64String(hashed);
    }
}

using System.Security.Cryptography;
using System.Text;

namespace NimbusWatchApi.Services
{
  public interface IPasswordService
  {
    string Encrypt(string? password);
    string Decrypt(string? password);
  }

  public class PasswordService : IPasswordService
  {
    private readonly string _masterKey = "dhsbuid3467812!!34272386fsdnguirghfh!231huiasdfty!21321&%¤#";

    private const int SaltSize = 32; // 256-bit
    private const int IvSize = 12; // 96-bit  (GCM standard)
    private const int TagSize = 16; // 128-bit
    private const int KeySize = 32; // 256-bit AES key
    private const int Iterations = 200_000;

    // Blob layout: [ Salt (32) | IV (12) | Tag (16) | CipherText (variable) ]

    /// <summary>
    /// Encrypts a password and returns a Base64-encoded string safe for storage.
    /// </summary>
    public string Encrypt(string? password)
    {
      if (string.IsNullOrEmpty(password))
        throw new ArgumentNullException(nameof(password));

      byte[] salt = RandomBytes(SaltSize);
      byte[] iv = RandomBytes(IvSize);
      byte[] key = DeriveKey(salt);

      byte[] plainBytes = Encoding.UTF8.GetBytes(password);
      byte[] cipherBytes = new byte[plainBytes.Length];
      byte[] tag = new byte[TagSize];

      using (var aesGcm = new AesGcm(key, TagSize))
        aesGcm.Encrypt(iv, plainBytes, cipherBytes, tag);

      return Convert.ToBase64String(Pack(salt, iv, tag, cipherBytes));
    }

    /// <summary>
    /// Decrypts a Base64-encoded string produced by <see cref="Encrypt"/>.
    /// Throws <see cref="CryptographicException"/> if the data is invalid or tampered with.
    /// </summary>
    public string Decrypt(string? encryptedBase64)
    {
      if (string.IsNullOrEmpty(encryptedBase64))
        throw new ArgumentNullException(nameof(encryptedBase64));

      byte[] blob = Convert.FromBase64String(encryptedBase64);

      (byte[] salt, byte[] iv, byte[] tag, byte[] cipherBytes) = Unpack(blob);

      byte[] key = DeriveKey(salt);
      byte[] plainBytes = new byte[cipherBytes.Length];

      using (var aesGcm = new AesGcm(key, TagSize))
        aesGcm.Decrypt(iv, cipherBytes, tag, plainBytes);

      return Encoding.UTF8.GetString(plainBytes);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private byte[] DeriveKey(byte[] salt)
    {
      using var kdf = new Rfc2898DeriveBytes(
          _masterKey, salt, Iterations, HashAlgorithmName.SHA512);
      return kdf.GetBytes(KeySize);
    }

    private static byte[] Pack(byte[] salt, byte[] iv, byte[] tag, byte[] cipher)
    {
      byte[] blob = new byte[SaltSize + IvSize + TagSize + cipher.Length];
      int offset = 0;
      Buffer.BlockCopy(salt, 0, blob, offset, SaltSize); offset += SaltSize;
      Buffer.BlockCopy(iv, 0, blob, offset, IvSize); offset += IvSize;
      Buffer.BlockCopy(tag, 0, blob, offset, TagSize); offset += TagSize;
      Buffer.BlockCopy(cipher, 0, blob, offset, cipher.Length);
      return blob;
    }

    private static (byte[] salt, byte[] iv, byte[] tag, byte[] cipher) Unpack(byte[] blob)
    {
      int minLength = SaltSize + IvSize + TagSize;
      if (blob.Length <= minLength)
        throw new CryptographicException("Invalid encrypted data.");

      int offset = 0;
      byte[] salt = blob[offset..(offset += SaltSize)];
      byte[] iv = blob[offset..(offset += IvSize)];
      byte[] tag = blob[offset..(offset += TagSize)];
      byte[] cipher = blob[offset..];

      return (salt, iv, tag, cipher);
    }

    private static byte[] RandomBytes(int length)
    {
      byte[] bytes = new byte[length];
      RandomNumberGenerator.Fill(bytes);
      return bytes;
    }
  }
}
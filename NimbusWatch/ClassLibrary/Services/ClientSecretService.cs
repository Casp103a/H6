using System.Security.Cryptography;

namespace NimbusWatchApi.Services
{
  public interface IClientSecretService
  {
    string HashSecret(string secret);
    bool VerifySecret(string secret, string storedHash);
  }

  public class ClientSecretService : IClientSecretService
  {
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string HashSecret(string secret)
    {
      if (string.IsNullOrWhiteSpace(secret))
        throw new ArgumentNullException(nameof(secret));

      var salt = RandomNumberGenerator.GetBytes(SaltSize);
      using var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, Iterations, HashAlgorithmName.SHA256);
      var hash = pbkdf2.GetBytes(KeySize);

      return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifySecret(string secret, string storedHash)
    {
      if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(storedHash))
        return false;

      var parts = storedHash.Split('.', 3);
      if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        return false;

      var salt = Convert.FromBase64String(parts[1]);
      var expectedHash = Convert.FromBase64String(parts[2]);

      using var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, iterations, HashAlgorithmName.SHA256);
      var actualHash = pbkdf2.GetBytes(expectedHash.Length);

      return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
  }
}

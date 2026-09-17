using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IRefreshTokenManager
  {
    Task<bool> Add(RefreshTokens refreshToken);
    Task<bool> Update(Guid tokenId, bool hasBeenUsed);
    Task<bool> Delete(Guid refreshTokenId);
    Task<RefreshTokens?> GetRefreshToken(Guid id, UserModel user);
    Task<bool> RotateRefreshToken(Guid usedTokenId, UserModel user, string expectedRefreshTokenHash, RefreshTokens newRefreshToken);
    Task<bool> RevokeAllForUser(UserModel user);
  }

  public class RefreshTokenManager : IRefreshTokenManager
  {
    private readonly IDatabaseManager databaseManager;

    public RefreshTokenManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> Delete(Guid refreshTokenId)
    {
      try
      {
        return await databaseManager.RefreshTokens.RemoveRefreshToken(refreshTokenId);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<RefreshTokens?> GetRefreshToken(Guid id, UserModel user)
    {
      return await databaseManager.RefreshTokens.GetRefreshToken(id, user.Id);
    }

    public async Task<bool> Add(RefreshTokens refreshToken)
    {
      try
      {
        return await databaseManager.RefreshTokens.AddRefreshToken(refreshToken);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> Update(Guid tokenId, bool hasBeenUsed)
    {
      try
      {
        return await databaseManager.RefreshTokens.UpdateRefreshToken(tokenId, hasBeenUsed);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> RotateRefreshToken(Guid usedTokenId, UserModel user, string expectedRefreshTokenHash, RefreshTokens newRefreshToken)
    {
      try
      {
        return await databaseManager.RefreshTokens.RotateRefreshToken(
          usedTokenId,
          user.Id,
          expectedRefreshTokenHash,
          newRefreshToken);
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> RevokeAllForUser(UserModel user)
    {
      try
      {
        await databaseManager.RefreshTokens.RevokeAllForUser(user.Id);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}

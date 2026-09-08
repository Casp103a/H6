using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface IRefreshTokenManager
  {
    Task<bool> Add(RefreshTokens refreshToken);
    Task<bool> Update(Guid TokenId, bool HasBeenUsed);
    Task<bool> Delete(Guid refreshTokenId);
    Task<RefreshTokens?> GetRefreshToken(Guid Id, UserModel user);
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
      var token = await databaseManager.RefreshTokens.GetAllRefreshTokens().ContinueWith(t => t.Result.FirstOrDefault(x => x.ID == refreshTokenId));

      if (token != null)
      {
        await databaseManager.RefreshTokens.RemoveRefreshToken(token.ID);
        return true;
      }

      return false;
    }

    public async Task<RefreshTokens?> GetRefreshToken(Guid Id, UserModel user)
    {
      var tokens = await databaseManager.RefreshTokens.GetAllRefreshTokens();
      return tokens.FirstOrDefault(x => x.ID == Id && x.UserId == user.Id);
    }

    public async Task<bool> Add(RefreshTokens refreshToken)
    {
      try
      {
        var result = await databaseManager.RefreshTokens.AddRefreshToken(refreshToken);
        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> Update(Guid TokenId, bool HasBeenUsed)
    {
      try
      {
        var token = await databaseManager.RefreshTokens.GetAllRefreshTokens().ContinueWith(t => t.Result.FirstOrDefault(x => x.ID == TokenId));

        if (token == null) return false;

        token.RefreshTokenIsUsed = HasBeenUsed;

        if (await databaseManager.RefreshTokens.UpdateRefreshToken(TokenId, HasBeenUsed))
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}

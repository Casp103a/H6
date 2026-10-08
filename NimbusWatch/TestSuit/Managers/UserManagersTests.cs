using ClassLibrary.Dtos;
using ClassLibrary.Services;
using FluentAssertions;
using NimbusWatchApi.Database;
using NimbusWatchApi.Database.TableManagers;
using NimbusWatchApi.Managers;
using NimbusWatchApi.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace TestSuit.Managers
{
  public class UserManagersTests
  {
    private readonly IDatabaseManager databaseManager;
    private readonly IUserTableManager users;
    private readonly IEncryptingService encryptionService;
    private readonly UserManager sut;

    public UserManagersTests()
    {
      databaseManager = Substitute.For<IDatabaseManager>();
      users = Substitute.For<IUserTableManager>();
      encryptionService = Substitute.For<IEncryptingService>();
      databaseManager.Users.Returns(users);
      sut = new UserManager(databaseManager, encryptionService);
    }

    private static UserModel CreateUser(int id = 1, string email = "test@test.dk") => new()
    {
      Id = id,
      Email = email,
      Password = "encrypted",
      RoleId = 2,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      CompanyId = "company-1",
      PublicId = Guid.NewGuid()
    };

    // CreateAsync

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenDtoIsNull()
    {
      var result = await sut.CreateAsync(null!);

      result.Should().BeNull();
      await users.DidNotReceive().AddUser(Arg.Any<UserModel>());
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("test@test.dk", "")]
    public async Task CreateAsync_ReturnsNull_WhenEmailOrPasswordEmpty(string email, string password)
    {
      var dto = new RegisterUserDto { Email = email, Password = password, RoleId = 1, CompanyId = "c" };

      var result = await sut.CreateAsync(dto);

      result.Should().BeNull();
      await users.DidNotReceive().AddUser(Arg.Any<UserModel>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsFalse_WhenUserAlreadyExists()
    {
      var dto = new RegisterUserDto { Email = "test@test.dk", Password = "password", RoleId = 1, CompanyId = "c" };
      users.GetUser(dto.Email).Returns(CreateUser());

      var result = await sut.CreateAsync(dto);

      result.Should().BeFalse();
      await users.DidNotReceive().AddUser(Arg.Any<UserModel>());
    }

    [Fact]
    public async Task CreateAsync_AddsEncryptedUser_AndReturnsTrue()
    {
      var dto = new RegisterUserDto { Email = "test@test.dk", Password = "password", RoleId = 3, CompanyId = "company-1" };
      users.GetUser(dto.Email).Returns((UserModel?)null);
      encryptionService.Encrypt("password").Returns("encrypted");

      var result = await sut.CreateAsync(dto);

      result.Should().BeTrue();
      await users.Received(1).AddUser(Arg.Is<UserModel>(u =>
        u.Email == dto.Email &&
        u.Password == "encrypted" &&
        u.RoleId == 3 &&
        u.CompanyId == "company-1" &&
        u.IsActive &&
        u.PublicId != Guid.Empty));
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenDatabaseThrows()
    {
      var dto = new RegisterUserDto { Email = "test@test.dk", Password = "password", RoleId = 1, CompanyId = "c" };
      users.GetUser(dto.Email).Throws(new Exception("db error"));

      var result = await sut.CreateAsync(dto);

      result.Should().BeNull();
    }

    // UpdateAsync

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenUserIsNull()
    {
      var result = await sut.UpdateAsync(null!);

      result.Should().BeNull();
      await users.DidNotReceive().UpdateUser(Arg.Any<UserModel>());
    }

    [Fact]
    public async Task UpdateAsync_DoesNotEncrypt_WhenPasswordNotChanged()
    {
      var user = CreateUser();

      var result = await sut.UpdateAsync(user);

      result.Should().BeTrue();
      user.Password.Should().Be("encrypted");
      encryptionService.DidNotReceive().Encrypt(Arg.Any<string?>());
      await users.Received(1).UpdateUser(user);
    }

    [Fact]
    public async Task UpdateAsync_EncryptsPassword_WhenPasswordChanged()
    {
      var user = CreateUser();
      user.Password = "newPassword";
      encryptionService.Encrypt("newPassword").Returns("newEncrypted");

      var result = await sut.UpdateAsync(user, passwordChanged: true);

      result.Should().BeTrue();
      user.Password.Should().Be("newEncrypted");
      await users.Received(1).UpdateUser(Arg.Is<UserModel>(u => u.Password == "newEncrypted"));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenDatabaseThrows()
    {
      var user = CreateUser();
      users.UpdateUser(user).Throws(new Exception("db error"));

      var result = await sut.UpdateAsync(user);

      result.Should().BeNull();
    }

    // DeleteAsync

    [Fact]
    public async Task DeleteAsync_DeletesUserById_AndReturnsTrue()
    {
      var user = CreateUser(id: 42);

      var result = await sut.DeleteAsync(user);

      result.Should().BeTrue();
      await users.Received(1).DeleteUser(42);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNull_WhenDatabaseThrows()
    {
      users.DeleteUser(Arg.Any<int>()).Throws(new Exception("db error"));

      var result = await sut.DeleteAsync(CreateUser());

      result.Should().BeNull();
    }

    // GetAllUsers

    [Fact]
    public async Task GetAllUsers_MapsUsersToDtos()
    {
      var u1 = CreateUser(1, "a@test.dk");
      var u2 = CreateUser(2, "b@test.dk");
      users.GetAllUsers("company-1").Returns(new List<UserModel> { u1, u2 });

      var result = await sut.GetAllUsers("company-1");

      result.Should().HaveCount(2);
      result.Should().BeEquivalentTo(new[]
      {
        new UserInfoDto { Id = u1.Id, Email = u1.Email, RoleId = u1.RoleId, PublicId = u1.PublicId },
        new UserInfoDto { Id = u2.Id, Email = u2.Email, RoleId = u2.RoleId, PublicId = u2.PublicId }
      });
    }

    [Fact]
    public async Task GetAllUsers_ReturnsEmptyList_WhenDatabaseThrows()
    {
      users.GetAllUsers(Arg.Any<string>()).Throws(new Exception("db error"));

      var result = await sut.GetAllUsers("company-1");

      result.Should().NotBeNull().And.BeEmpty();
    }

    // CheckPasswordSignInAsync

    [Fact]
    public async Task CheckPasswordSignInAsync_ReturnsTrue_WhenPasswordMatches()
    {
      var login = new LoginRequestDto { Email = "test@test.dk", Password = "password" };
      users.GetUser(login.Email).Returns(CreateUser());
      encryptionService.Decrypt("encrypted").Returns("password");

      var result = await sut.CheckPasswordSignInAsync(login, "password");

      result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_ReturnsFalse_WhenPasswordDoesNotMatch()
    {
      var login = new LoginRequestDto { Email = "test@test.dk", Password = "wrong" };
      users.GetUser(login.Email).Returns(CreateUser());
      encryptionService.Decrypt("encrypted").Returns("password");

      var result = await sut.CheckPasswordSignInAsync(login, "wrong");

      result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_ReturnsFalse_WhenUserNotFound()
    {
      var login = new LoginRequestDto { Email = "missing@test.dk", Password = "password" };
      users.GetUser(login.Email).Returns((UserModel?)null);

      var result = await sut.CheckPasswordSignInAsync(login, "password");

      result.Should().BeFalse();
      encryptionService.DidNotReceive().Decrypt(Arg.Any<string?>());
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_ReturnsFalse_WhenDatabaseThrows()
    {
      var login = new LoginRequestDto { Email = "test@test.dk", Password = "password" };
      users.GetUser(login.Email).Throws(new Exception("db error"));

      var result = await sut.CheckPasswordSignInAsync(login, "password");

      result.Should().BeFalse();
    }

    // GetUser

    [Fact]
    public async Task GetUserById_ReturnsDto_WhenUserExists()
    {
      var user = CreateUser(5);
      users.GetUser(5).Returns(user);

      var result = await sut.GetUser(5);

      result.Should().BeEquivalentTo(new UserInfoDto { Id = 5, Email = user.Email, RoleId = user.RoleId, PublicId = user.PublicId });
    }

    [Fact]
    public async Task GetUserById_ReturnsNull_WhenUserNotFound()
    {
      users.GetUser(5).Returns((UserModel?)null);

      var result = await sut.GetUser(5);

      result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserById_ReturnsNull_WhenDatabaseThrows()
    {
      users.GetUser(Arg.Any<int>()).Throws(new Exception("db error"));

      var result = await sut.GetUser(5);

      result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsDto_WhenUserExists()
    {
      var user = CreateUser(7, "x@test.dk");
      users.GetUser("x@test.dk").Returns(user);

      var result = await sut.GetUser("x@test.dk");

      result.Should().BeEquivalentTo(new UserInfoDto { Id = 7, Email = "x@test.dk", RoleId = user.RoleId, PublicId = user.PublicId });
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsNull_WhenUserNotFound()
    {
      users.GetUser("x@test.dk").Returns((UserModel?)null);

      var result = await sut.GetUser("x@test.dk");

      result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsNull_WhenDatabaseThrows()
    {
      users.GetUser(Arg.Any<string>()).Throws(new Exception("db error"));

      var result = await sut.GetUser("x@test.dk");

      result.Should().BeNull();
    }

    // GetUserModel

    [Fact]
    public async Task GetUserModelByEmail_ReturnsModel()
    {
      var user = CreateUser();
      users.GetUser(user.Email).Returns(user);

      var result = await sut.GetUserModel(user.Email);

      result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task GetUserModelByEmail_ReturnsNull_WhenDatabaseThrows()
    {
      users.GetUser(Arg.Any<string>()).Throws(new Exception("db error"));

      var result = await sut.GetUserModel("x@test.dk");

      result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserModelById_ReturnsModel()
    {
      var user = CreateUser(9);
      users.GetUser(9).Returns(user);

      var result = await sut.GetUserModel(9);

      result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task GetUserModelById_ReturnsNull_WhenDatabaseThrows()
    {
      users.GetUser(Arg.Any<int>()).Throws(new Exception("db error"));

      var result = await sut.GetUserModel(9);

      result.Should().BeNull();
    }

    // CheckIfCompanyHasAUser

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckIfCompanyHasAUser_ReturnsDatabaseResult(bool expected)
    {
      users.CheckIfCompanyHasAUser("company-1").Returns(expected);

      var result = await sut.CheckIfCompanyHasAUser("company-1");

      result.Should().Be(expected);
    }

    [Fact]
    public async Task CheckIfCompanyHasAUser_ReturnsFalse_WhenDatabaseThrows()
    {
      users.CheckIfCompanyHasAUser(Arg.Any<string>()).Throws(new Exception("db error"));

      var result = await sut.CheckIfCompanyHasAUser("company-1");

      result.Should().BeFalse();
    }
  }
}

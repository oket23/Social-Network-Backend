using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Application.Users;

public interface IUserRepository : IRepository<User>
{
    Task<bool> IsUsernameTaken(string username, Guid exceptUserId, CancellationToken ct = default);

    Task<User?> FindByPhoneNumber(string phoneNumber, CancellationToken ct = default);
}

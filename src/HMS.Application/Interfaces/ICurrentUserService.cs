namespace HMS.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsInRole(string role);
}

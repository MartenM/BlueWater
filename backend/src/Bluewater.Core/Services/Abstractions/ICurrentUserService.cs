namespace Bluewater.Core.Services.Abstractions;

public interface ICurrentUserService
{
    public Guid Id { get; set; }
    public string Username { get; set; }
}
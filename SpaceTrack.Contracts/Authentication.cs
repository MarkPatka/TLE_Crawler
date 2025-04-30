namespace SpaceTrack.Contracts
{
    public record LoginRequest(string Identity, string Password);
    public record LoginResponse();
}

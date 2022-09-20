namespace Niarru.Zodchy.Server.Data;

//Client credentials is analogue for login/password pair, we send it to the client after registration
public struct PlayerCredentials
{
    public long Id { get; }
    public string Token { get; }

    public PlayerCredentials(long id, string token)
    {
        this.Id = id;
        this.Token = token;
    }
}
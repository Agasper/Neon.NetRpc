namespace Niarru.Zodchy.Server.Data
{

    public class PlayerNotFoundException : Exception
    {
        public PlayerNotFoundException(long id) : base($"Player with id #{id} and specified token not found in the DB")
        {
        }
    }

}
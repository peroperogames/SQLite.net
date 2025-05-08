
namespace SQLite
{
    public interface IRollbackHandler
    {
        void OnRollback();
    }
}

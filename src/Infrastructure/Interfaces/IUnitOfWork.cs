using Infrastructure.Repositories;

namespace Infrastructure.Interfaces
{
    public interface IUnitOfWork
    {
        GameRepository GameRepo { get; }
        MyGameRepository MyGameRepo { get; }
        void Save();
        Task SaveAsync();
    }
}
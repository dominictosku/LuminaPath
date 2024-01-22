using Infrastructure.Repositories;

namespace Infrastructure.Interfaces
{
    public interface IUnitOfWork
    {
        GameRepo GameRepo { get; }
        MyGameRepo MyGameRepo { get; }

        void Dispose();
        void Save();
        Task SaveAsync();
    }
}
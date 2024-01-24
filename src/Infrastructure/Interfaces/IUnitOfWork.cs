using Infrastructure.Repositories;

namespace Infrastructure.Interfaces
{
    public interface IUnitOfWork
    {
        GameRepo GameRepo { get; }
        MyGameRepo MyGameRepo { get; }
        void Save();
        Task SaveAsync();
    }
}
namespace Domain.Common.Interfaces
{
	public interface IUnitOfWork
	{
		IGameRepository GameRepo { get; }
		IMyGameRepository MyGameRepo { get; }
		void Save();
		Task SaveAsync();
	}
}
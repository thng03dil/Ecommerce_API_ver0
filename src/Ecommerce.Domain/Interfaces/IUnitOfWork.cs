namespace Ecommerce.Domain.Interfaces
{
    /// <summary>
    /// Provides transactional execution for multiple persistence operations.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Executes the action within a database transaction. On success, commits; on exception, rolls back.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    }
}

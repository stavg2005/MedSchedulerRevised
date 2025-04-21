using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    public abstract class BaseRepository<TEntity, TKey> where TEntity : class // TEntity must be a class
    {
        protected readonly string ConnectionString; // Accessible to derived classes

        protected BaseRepository(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            // Optional: Configure Dapper type handlers here if needed globally
        }

        // Shared helper to create and open a connection
        protected async Task<IDbConnection> CreateOpenConnectionAsync()
        {
            var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        // Abstract methods defining the contract for all repositories
        public abstract Task<TEntity> GetByIdAsync(TKey id); // Return null if not found
        public abstract Task<List<TEntity>> GetAllAsync();
        public abstract Task AddAsync(TEntity entity);
        public abstract Task<bool> UpdateAsync(TEntity entity); // Return bool indicating success?
        public abstract Task<bool> DeleteAsync(TKey id); // Return bool indicating success?

        // You might add other common methods if applicable, e.g.:
        // public abstract Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TKey> ids);
    }
}

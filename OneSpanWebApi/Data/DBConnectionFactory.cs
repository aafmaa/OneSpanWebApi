using System.Data;
using Microsoft.Data.SqlClient;

namespace OneSpanWebApi.Data
{
    public class DBConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public DBConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        }
    }
}

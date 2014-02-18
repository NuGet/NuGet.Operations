using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuCmd
{
    public static class SqlConnectionExtensions
    {
        public static async Task<DataTable> QueryDatatable(this SqlConnection self, string query, params SqlParameter[] parameters)
        {
            using (var cmd = self.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddRange(parameters);
                var reader = await cmd.ExecuteReaderAsync();
                DataTable table = new DataTable();
                table.Load(reader);
                return table;
            }
        }
    }
}

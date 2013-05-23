using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderBlobs
{
    class Warehouse
    {
        public static IDictionary<string, int> GetRanking(string warehouseSqlConnectionString)
        {
            string sql = @"
                SELECT TOP(100) Dimension_Package.PackageId, SUM(DownloadCount) 'Downloads'
                FROM Fact_Download
                INNER JOIN Dimension_Package ON Dimension_Package.Id = Fact_Download.Dimension_Package_Id
                INNER JOIN Dimension_Date ON Dimension_Date.Id = Fact_Download.Dimension_Date_Id
                WHERE Dimension_Date.[Date] >= CONVERT(DATE, DATEADD(day, -42, GETDATE()))
                  AND Dimension_Date.[Date] < CONVERT(DATE, GETDATE())
                GROUP BY Dimension_Package.PackageId
                ORDER BY SUM(DownloadCount) DESC
            ";

            IDictionary<string, int> result = new Dictionary<string, int>();

            using (SqlConnection connection = new SqlConnection(warehouseSqlConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sql, connection);

                SqlDataReader reader = command.ExecuteReader();

                int index = 0;

                while (reader.Read())
                {
                    index++;

                    string id = (string)reader.GetValue(0);

                    string key = string.Format("{0}", id);

                    result.Add(key, index);
                }
            }

            return result;
        }
    }
}


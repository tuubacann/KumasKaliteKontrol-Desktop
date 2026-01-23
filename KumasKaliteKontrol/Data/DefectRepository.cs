using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using KumasKaliteKontrol.Models;

namespace KumasKaliteKontrol.Data
{
    public class DefectRepository
    {
        public void AddDefect(Defect defect)
        {
            using var con = DbContext.CreateConnection();
            con.Open();

            string sql = @"
                INSERT INTO Defects (FabricId, StartMeter, EndMeter, PointType, Length)
                VALUES (@fabricId, @start, @end, @point, @length);
            ";

            var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@fabricId", defect.FabricId);
            cmd.Parameters.AddWithValue("@start", defect.StartMeter);
            cmd.Parameters.AddWithValue("@end", defect.EndMeter);
            cmd.Parameters.AddWithValue("@point", defect.PointType);
            cmd.Parameters.AddWithValue("@length", defect.Length);

            cmd.ExecuteNonQuery();
        }
    }
}


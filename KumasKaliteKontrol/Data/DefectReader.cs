using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using KumasKaliteKontrol.Models;
using System.Collections.Generic;

namespace KumasKaliteKontrol.Data
{
    public class DefectReader
    {
        public List<Defect> GetDefectsByFabric(int fabricId)
        {
            using var con = DbContext.CreateConnection();
            con.Open();

            string sql = "SELECT * FROM Defects WHERE FabricId = @id";
            var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", fabricId);

            var reader = cmd.ExecuteReader();

            List<Defect> defects = new List<Defect>();

            while (reader.Read())
            {
                defects.Add(new Defect
                {
                    Id = reader.GetInt32(0),
                    FabricId = reader.GetInt32(1),
                    StartMeter = reader.GetInt32(2),
                    EndMeter = reader.GetInt32(3),
                    PointType = reader.GetInt32(4),
                    Length = reader.GetInt32(5)
                });
            }

            return defects;
        }
    }
}


using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using KumasKaliteKontrol.Models;

namespace KumasKaliteKontrol.Data
{
    public class PartyRepository
    {
        public void AddParty(Party party)
        {
            using var con = DbContext.CreateConnection();
            con.Open();

            string sql = @"
                INSERT INTO Parties 
                    (FabricId, StartMeter, EndMeter, Length, TotalPoints, Quality)
                VALUES 
                    (@fabricId, @start, @end, @length, @points, @quality);
            ";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@fabricId", party.FabricId);
            cmd.Parameters.AddWithValue("@start", party.StartMeter);
            cmd.Parameters.AddWithValue("@end", party.EndMeter);
            cmd.Parameters.AddWithValue("@length", party.Length);
            cmd.Parameters.AddWithValue("@points", party.TotalPoints);
            cmd.Parameters.AddWithValue("@quality", (int)party.Quality);

            cmd.ExecuteNonQuery();
        }

        public void DeletePartiesByFabric(int fabricId)
        {
            using var con = DbContext.CreateConnection();
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Parties WHERE FabricId = @fid";
            cmd.Parameters.AddWithValue("@fid", fabricId);

            cmd.ExecuteNonQuery();
        }

        public List<Party> GetPartiesByFabric(int fabricId)
        {
            var list = new List<Party>();

            using var con = DbContext.CreateConnection();
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    Id, FabricId, StartMeter, EndMeter, Length, TotalPoints, Quality
                FROM Parties
                WHERE FabricId = @fid
                ORDER BY StartMeter;
            ";
            cmd.Parameters.AddWithValue("@fid", fabricId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Party
                {
                    Id = r.GetInt32(0),
                    FabricId = r.GetInt32(1),
                    StartMeter = r.GetInt32(2),
                    EndMeter = r.GetInt32(3),
                    Length = r.GetInt32(4),
                    TotalPoints = r.GetInt32(5),
                    Quality = (QualityLevel)r.GetInt32(6)
                });
            }

            return list;
        }

        public List<Party> GetAllParties()
        {
            var list = new List<Party>();

            using var con = DbContext.CreateConnection();
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    Id, FabricId, StartMeter, EndMeter, Length, TotalPoints, Quality
                FROM Parties
                ORDER BY Id DESC;
            ";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Party
                {
                    Id = r.GetInt32(0),
                    FabricId = r.GetInt32(1),
                    StartMeter = r.GetInt32(2),
                    EndMeter = r.GetInt32(3),
                    Length = r.GetInt32(4),
                    TotalPoints = r.GetInt32(5),
                    Quality = (QualityLevel)r.GetInt32(6)
                });
            }

            return list;
        }
    }
}

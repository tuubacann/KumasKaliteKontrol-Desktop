using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using KumasKaliteKontrol.Models;

namespace KumasKaliteKontrol.Data
{
    public class FabricRepository
    {
        public int AddFabric(Fabric fabric)
        {
            using var con = DbContext.CreateConnection();
            con.Open();

            string sql = @"
                INSERT INTO Fabrics (Name, Code, TotalMeters)
                VALUES (@name, @code, @meters);
                SELECT last_insert_rowid();
            ";

            var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@name", fabric.Name);
            cmd.Parameters.AddWithValue("@code", fabric.Code);
            cmd.Parameters.AddWithValue("@meters", fabric.TotalMeters);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Fabric> GetAllFabrics()
        {
            using var con = DbContext.CreateConnection();
            con.Open();

            string sql = "SELECT Id, Name, Code, TotalMeters FROM Fabrics ORDER BY Id DESC";
            var cmd = new Microsoft.Data.Sqlite.SqliteCommand(sql, con);

            var reader = cmd.ExecuteReader();
            var list = new List<Fabric>();

            while (reader.Read())
            {
                list.Add(new Fabric
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Code = reader.GetString(2),
                    TotalMeters = reader.GetInt32(3)
                });
            }

            return list;
        }

        public Fabric? GetFabricById(int id)
        {
            using var con = DbContext.CreateConnection();

            string sql = "SELECT Id, Name, Code, TotalMeters FROM Fabrics WHERE Id=@id";
            var cmd = new Microsoft.Data.Sqlite.SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", id);

            var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new Fabric
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Code = reader.GetString(2),
                TotalMeters = reader.GetInt32(3)
            };
        }

    }
}


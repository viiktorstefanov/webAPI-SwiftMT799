using Microsoft.Data.Sqlite;
using System;

public static class Database
{
    private const string ConnectionString = "Data Source=messages.db";

    public static void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Block1 TEXT,
                Block2 TEXT,
                Block4 TEXT,
                Block5 TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
            command.ExecuteNonQuery();
        }
    }

    public static void SaveMessage(string block1, string block2, string block4, string block5)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Messages (Block1, Block2, Block4, Block5)
            VALUES ($block1, $block2, $block4, $block5)";
            command.Parameters.AddWithValue("$block1", block1);
            command.Parameters.AddWithValue("$block2", block2);
            command.Parameters.AddWithValue("$block4", block4);
            command.Parameters.AddWithValue("$block5", block5);
            command.ExecuteNonQuery();
        }
    }
}

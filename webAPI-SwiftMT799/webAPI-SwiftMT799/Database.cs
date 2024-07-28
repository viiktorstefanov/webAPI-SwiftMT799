using Microsoft.Data.Sqlite;
using System;
using static MessageController;

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
                MessageID INTEGER PRIMARY KEY AUTOINCREMENT,
                TypeOfMessage TEXT,
                ServiceLevel TEXT,
                BIC TEXT,
                SessionNumber TEXT,
                SequenceNumber TEXT,
                MessageDirection TEXT,
                MessageType TEXT,
                ReceiverBIC TEXT,
                SenderBIC TEXT,
                AppHeaderSessionNumber TEXT,
                AppHeaderSequenceNumber TEXT,
                MessagePriority TEXT,
                TransactionRef TEXT,
                RelatedRef TEXT,
                MessageText TEXT,
                Checksum TEXT,
                DigitalSignature TEXT,
                Timestamp TEXT
            )";
            command.ExecuteNonQuery();
        }
    }

    public static void SaveMessage(MessageEntity messageEntity)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Messages (
                TypeOfMessage,
                ServiceLevel,
                BIC,
                SessionNumber,
                SequenceNumber,
                MessageDirection,
                MessageType,
                ReceiverBIC,
                SenderBIC,
                AppHeaderSessionNumber,
                AppHeaderSequenceNumber,
                MessagePriority,
                TransactionRef,
                RelatedRef,
                MessageText,
                Checksum,
                DigitalSignature,
                Timestamp
            ) VALUES (
                @TypeOfMessage,
                @ServiceLevel,
                @BIC,
                @SessionNumber,
                @SequenceNumber,
                @MessageDirection,
                @MessageType,
                @ReceiverBIC,
                @SenderBIC,
                @AppHeaderSessionNumber,
                @AppHeaderSequenceNumber,
                @MessagePriority,
                @TransactionRef,
                @RelatedRef,
                @MessageText,
                @Checksum,
                @DigitalSignature,
                @Timestamp
            )";

            command.Parameters.AddWithValue("@TypeOfMessage", messageEntity.TypeOfMessage);
            command.Parameters.AddWithValue("@ServiceLevel", messageEntity.ServiceLevel);
            command.Parameters.AddWithValue("@BIC", messageEntity.BIC);
            command.Parameters.AddWithValue("@SessionNumber", messageEntity.SessionNumber);
            command.Parameters.AddWithValue("@SequenceNumber", messageEntity.SequenceNumber);
            command.Parameters.AddWithValue("@MessageDirection", messageEntity.MessageDirection);
            command.Parameters.AddWithValue("@MessageType", messageEntity.MessageType);
            command.Parameters.AddWithValue("@ReceiverBIC", messageEntity.ReceiverBIC);
            command.Parameters.AddWithValue("@SenderBIC", messageEntity.SenderBIC);
            command.Parameters.AddWithValue("@AppHeaderSessionNumber", messageEntity.AppHeaderSessionNumber);
            command.Parameters.AddWithValue("@AppHeaderSequenceNumber", messageEntity.AppHeaderSequenceNumber);
            command.Parameters.AddWithValue("@MessagePriority", messageEntity.MessagePriority);
            command.Parameters.AddWithValue("@TransactionRef", messageEntity.TransactionRef);
            command.Parameters.AddWithValue("@RelatedRef", messageEntity.RelatedRef);
            command.Parameters.AddWithValue("@MessageText", messageEntity.MessageText);
            command.Parameters.AddWithValue("@Checksum", messageEntity.Checksum);
            command.Parameters.AddWithValue("@DigitalSignature", messageEntity.DigitalSignature);
            command.Parameters.AddWithValue("@Timestamp", messageEntity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

            command.ExecuteNonQuery();
        }
    }
}


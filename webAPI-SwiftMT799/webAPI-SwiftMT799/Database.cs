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

    public static List<MessageEntity> GetAllMessages()
    {
        var messages = new List<MessageEntity>();

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Messages";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var message = new MessageEntity
                    {
                        messageId = reader.GetInt32(reader.GetOrdinal("MessageID")),
                        TypeOfMessage = reader.GetString(reader.GetOrdinal("TypeOfMessage")),
                        ServiceLevel = reader.GetString(reader.GetOrdinal("ServiceLevel")),
                        BIC = reader.GetString(reader.GetOrdinal("BIC")),
                        SessionNumber = reader.GetString(reader.GetOrdinal("SessionNumber")),
                        SequenceNumber = reader.GetString(reader.GetOrdinal("SequenceNumber")),
                        MessageDirection = reader.GetString(reader.GetOrdinal("MessageDirection")),
                        MessageType = reader.GetString(reader.GetOrdinal("MessageType")),
                        ReceiverBIC = reader.GetString(reader.GetOrdinal("ReceiverBIC")),
                        SenderBIC = reader.GetString(reader.GetOrdinal("SenderBIC")),
                        AppHeaderSessionNumber = reader.GetString(reader.GetOrdinal("AppHeaderSessionNumber")),
                        AppHeaderSequenceNumber = reader.GetString(reader.GetOrdinal("AppHeaderSequenceNumber")),
                        MessagePriority = reader.GetString(reader.GetOrdinal("MessagePriority")),
                        TransactionRef = reader.GetString(reader.GetOrdinal("TransactionRef")),
                        RelatedRef = reader.GetString(reader.GetOrdinal("RelatedRef")),
                        MessageText = reader.GetString(reader.GetOrdinal("MessageText")),
                        Checksum = reader.GetString(reader.GetOrdinal("Checksum")),
                        DigitalSignature = reader.GetString(reader.GetOrdinal("DigitalSignature")),
                        Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp")))
                    };

                    messages.Add(message);
                }
            }
        }

        return messages;
    }
}


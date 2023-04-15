using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;

namespace qqbot
{
    public class MessageRankItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
    public class PersonalMessageRankItem
    {
        public string Content { get; set; } = string.Empty;
        public int Count { get; set; }
    }
    public class DbHelper : IDisposable
    {
        private string connectionString;
        private MySqlConnection connection;
        private void CheckConnection()
        {
            if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
            {
                Logger.Error("数据库连接断开，正在重新连接!");
                connection.Close();
                connection.Open();
                Logger.Info("数据库重新连接成功!");
            }
        }
        public DbHelper(string connectionString)
        {
            this.connectionString = connectionString;
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }
        public DbHelper(Action<MySqlConnectionStringBuilder> build)
        {
            var builder = new MySqlConnectionStringBuilder();
            build(builder);
            this.connectionString = builder.ToString();
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }
        public async Task<string> GetInformationAsync(string key)
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT * FROM information WHERE `key`=@key";
            command.Parameters.AddWithValue("@key", key);
            
            using var reader = await command.ExecuteReaderAsync();
            reader.Read();
            return reader.GetString(1);
        }
        public async Task<int> UpdateInformationAsync(string key, string value)
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "UPDATE information SET `value`=@value WHERE `key`=@key";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            return await command.ExecuteNonQueryAsync();
        }
        public async Task<int> InsertGroupMessageAsync(Mirai.Net.Data.Messages.Receivers.GroupMessageReceiver e, string readableString)
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "INSERT INTO group_message (time, group_id, group_name, sender_id, sender_name, content)"
                + "VALUES (@time, @group_id, @group_name, @sender_id, @sender_name, @content)";
            var source = e.MessageChain.First() as Mirai.Net.Data.Messages.Concretes.SourceMessage;
            command.Parameters.AddWithValue("@time", DateTimeConverter.ToDateTime(source!.Time).ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@group_id", e.GroupId);
            command.Parameters.AddWithValue("@group_name", e.GroupName);
            command.Parameters.AddWithValue("@sender_id", e.Sender.Id);
            command.Parameters.AddWithValue("@sender_name", e.Sender.Name);
            command.Parameters.AddWithValue("@content", readableString);
            return await command.ExecuteNonQueryAsync();
        }
        public async IAsyncEnumerable<MessageRankItem> GetMessageRanksAsync(string GroupId)
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT sender_id, sender_name, COUNT(*) FROM group_message WHERE group_id=@group_id GROUP BY sender_id ORDER BY COUNT(*) DESC";
            command.Parameters.AddWithValue("@group_id", GroupId);
            using var reader = await command.ExecuteReaderAsync();

            while (reader.Read())
            {
                yield return new MessageRankItem()
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Count = reader.GetInt32(2)
                };
            }
        }
        public async IAsyncEnumerable<PersonalMessageRankItem> GetPersonalMessageRanksAsync(string GroupId, string SenderId, int Limit)
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT content, COUNT(*) FROM group_message WHERE `sender_id`=@sender_id AND `group_id`=@group_id GROUP BY content ORDER BY COUNT(*) DESC LIMIT @limit";
            command.Parameters.AddWithValue("@group_id", GroupId);
            command.Parameters.AddWithValue("@sender_id", SenderId);
            command.Parameters.AddWithValue("@limit", Limit);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                yield return new PersonalMessageRankItem()
                {
                    Content = reader.GetString(0),
                    Count = reader.GetInt32(1)
                };
            }
        }
        public async Task<int> GetMessageCountAsync(string? GroupId = null)
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT COUNT(*) FROM group_message";
            if (GroupId != null)
            {
                command.CommandText += $" WHERE `group_id`=@group_id";
                command.Parameters.AddWithValue("@group_id", GroupId);
            }

            using var reader = await command.ExecuteReaderAsync();
            reader.Read();
            return reader.GetInt32(0);
        }
        public async Task<int> GetHandledMessageCountAsync()
        {
            CheckConnection();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT COUNT(*) FROM group_message WHERE `content` LIKE '@2628754644%'";

            using var reader = await command.ExecuteReaderAsync();
            reader.Read();
            return reader.GetInt32(0);
        }
        public void Dispose()
        {
            connection.Dispose();
        }
    }
}

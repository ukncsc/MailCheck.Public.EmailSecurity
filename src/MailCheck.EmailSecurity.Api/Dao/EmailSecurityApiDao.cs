using System.Threading.Tasks;
using MailCheck.EmailSecurity.Api.Domain;
using Newtonsoft.Json;
using MailCheck.EmailSecurity.Entity.Config;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using BsonReader = Newtonsoft.Json.Bson.BsonReader;

namespace MailCheck.EmailSecurity.Api.Dao
{
    public interface IEmailSecurityApiDao
    {
        Task<EmailSecurityInfoResponse> Read(string domain);
    }

    public class EmailSecurityApiDao : IEmailSecurityApiDao
    {
        private readonly IDocumentDbConfig _config;
        private readonly IMongoClientProvider _mongoClientProvider;
        private const string CollectionName = "entities";

        public EmailSecurityApiDao(IDocumentDbConfig config,
            IMongoClientProvider mongoClientProvider)
        {
            _config = config;
            _mongoClientProvider = mongoClientProvider;
        }

        public async Task<EmailSecurityInfoResponse> Read(string domain)
        {
            IMongoCollection<RawBsonDocument> collection = await GetCollection();

            BsonDocument filter = new BsonDocument
            {
                ["Domain"] = domain
            };

            RawBsonDocument doc = await collection
                .Find(filter)
                .FirstOrDefaultAsync();

            if (doc != null)
            {
                using (ByteBufferStream stream = new ByteBufferStream(doc.Slice))
                using (BsonReader reader = new Newtonsoft.Json.Bson.BsonReader(stream))
                {
                    JsonSerializer ser = new JsonSerializer();
                    return ser.Deserialize<EmailSecurityInfoResponse>(reader);
                }
            }

            return null;
        }

        private async Task<IMongoCollection<RawBsonDocument>> GetCollection()
        {
            IMongoClient client = await _mongoClientProvider.GetMongoClient();

            IMongoDatabase database = client.GetDatabase(_config.Database);
            IMongoCollection<RawBsonDocument> collection = database.GetCollection<RawBsonDocument>(CollectionName);
            var indexKeys = new BsonDocument
            {
                ["Domain"] = 1
            };
            BsonDocumentIndexKeysDefinition<RawBsonDocument> keysDefinition =
                new BsonDocumentIndexKeysDefinition<RawBsonDocument>(indexKeys);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<RawBsonDocument>(keysDefinition, new CreateIndexOptions { Unique = true }));
            return collection;
        }
    }
}
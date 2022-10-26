using System.IO;
using System.Threading.Tasks;
using MailCheck.EmailSecurity.Entity.Config;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.Common.Util;

namespace MailCheck.EmailSecurity.Entity.Dao
{
    public interface IEmailSecurityEntityDao
    {
        Task Create(EmailSecurityEntityState state);
        Task<EmailSecurityEntityState> Read(string domain);
        Task Update(EmailSecurityEntityState state);
        Task Delete(string domain);
    }

    public class EmailSecurityEntityDao : IEmailSecurityEntityDao
    {
        private readonly IDocumentDbConfig _config;
        private readonly ILogger<EmailSecurityEntityDao> _logger;
        private readonly IClock _clock;
        private readonly IMongoClientProvider _mongoClientProvider;
        private const string CollectionName = "entities";

        public EmailSecurityEntityDao(IDocumentDbConfig config,
            ILogger<EmailSecurityEntityDao> logger,
            IClock clock,
            IMongoClientProvider mongoClientProvider)
        {
            _config = config;
            _logger = logger;
            _clock = clock;
            _mongoClientProvider = mongoClientProvider;
        }

        public async Task Create(EmailSecurityEntityState state)
        {
            DateTime currentTime = _clock.GetDateTimeUtc();
            state.CreatedAt = currentTime;
            state.UpdatedAt = currentTime;
            
            IMongoCollection<RawBsonDocument> collection = await GetCollection();
            
            await collection.InsertOneAsync(Serialize(state));
        }

        public async Task<EmailSecurityEntityState> Read(string domain)
        {
            IMongoCollection<RawBsonDocument> collection = await GetCollection();

            var filter = new BsonDocument
            {
                ["Domain"] = domain
            };
            try
            {
                RawBsonDocument doc = await collection
                    .Find(filter)
                    .FirstAsync();
                using (var stream = new ByteBufferStream(doc.Slice))
                using (var reader = new Newtonsoft.Json.Bson.BsonReader(stream))
                {
                    var ser = new JsonSerializer();
                    return ser.Deserialize<EmailSecurityEntityState>(reader);
                }
            }
            catch (InvalidOperationException e)
            {
                return null;
            }
        }

        public async Task Update(EmailSecurityEntityState state)
        {
            int prevVersion = state.Version;
            state.Version ++;
            DateTime currentTime = _clock.GetDateTimeUtc();
            state.UpdatedAt = currentTime;
            IMongoCollection<RawBsonDocument> collection = await GetCollection();

            var rawBsonDocument = Serialize(state);
            
            var filter = new BsonDocument
            {
                ["Domain"] = state.Domain,
                ["Version"] = prevVersion
            };
            var result = await collection.ReplaceOneAsync(filter, rawBsonDocument);
            if (result.IsAcknowledged && result.ModifiedCount == 1)
            {
                return;
            }

            throw new Exception($"Failed to Update entity for {state.Domain}, version mismatch occured.");
        }
        
        public async Task Delete(string domain)
        {
            IMongoCollection<RawBsonDocument> collection = await GetCollection();
            
            var filter = new BsonDocument
            {
                ["Domain"] = domain,
            };
            await collection.FindOneAndDeleteAsync(filter);
        }

        private static RawBsonDocument Serialize(EmailSecurityEntityState state)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            using (BsonDataWriter writer = new BsonDataWriter(ms))
            {
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.Serialize(writer, state);
                writer.Flush();
                data = ms.ToArray();
            }

            RawBsonDocument rawDocument = new RawBsonDocument(data);
            return rawDocument;
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
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<RawBsonDocument>(keysDefinition, new CreateIndexOptions{Unique = true}));
            return collection;
        }
    }
}

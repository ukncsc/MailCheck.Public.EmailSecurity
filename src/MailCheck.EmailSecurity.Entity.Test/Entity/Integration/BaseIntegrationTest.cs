using System.IO;
using System.Threading.Tasks;
using MailCheck.EmailSecurity.Entity.Domain;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.Integration
{
    [TestFixture(Category = "Integration")]
    public class BaseIntegrationTest
    {
        internal static MongoDbRunner Runner;
        private MongoClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Runner = MongoDbRunner.Start();
            _client = new MongoClient(Runner.ConnectionString);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Runner.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            IMongoDatabase database = _client.GetDatabase("test");
            IMongoCollection<EmailSecurityEntityState> collection = database.GetCollection<EmailSecurityEntityState>("entities");
            collection.DeleteMany(Builders<EmailSecurityEntityState>.Filter.Empty);
        }

        protected async Task StoreEntity(EmailSecurityEntityState entity)
        {
            IMongoDatabase database = _client.GetDatabase("test");
            IMongoCollection<RawBsonDocument> collection = database.GetCollection<RawBsonDocument>("entities");
            await collection.InsertOneAsync(Serialize(entity));
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
    }
}
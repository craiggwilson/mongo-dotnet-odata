using MongoDB.Driver;

namespace MongoDB.OData.Typed
{
    public sealed class TypedDataSource
    {
        public object DataContext { get; private set; }

        public MongoServer Server { get; private set; }

        public TypedDataSource(MongoServer server, object dataContext)
        {
            Server = server;
            DataContext = dataContext;
        }
    }
}
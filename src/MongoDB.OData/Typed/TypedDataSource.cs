using MongoDB.Driver;

namespace MongoDB.OData.Typed
{
    /// <summary>
    /// A data source that includes both the MongoServer needed to connect to MongoDB as well 
    /// as the defining DataContext provided by the user.
    /// </summary>
    public sealed class TypedDataSource
    {
        /// <summary>
        /// Gets the data context.
        /// </summary>
        /// <value>
        /// The data context.
        /// </value>
        public object DataContext { get; private set; }

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public MongoServer Server { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedDataSource" /> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="dataContext">The data context.</param>
        public TypedDataSource(MongoServer server, object dataContext)
        {
            Server = server;
            DataContext = dataContext;
        }
    }
}
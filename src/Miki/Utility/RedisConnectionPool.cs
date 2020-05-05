namespace Miki.Utility
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using StackExchange.Redis;
    using Miki.Logging;
    using StackExchange.Redis.Profiling;
    using System.Security.Cryptography;

    public class RedisConnection : IConnectionMultiplexer
    {
        private readonly IConnectionMultiplexer connection;

        public RedisConnection(IConnectionMultiplexer connection)
        {
            this.connection = connection;

            connection.ErrorMessage += ErrorMessage;
            connection.ConnectionFailed += ConnectionFailed;
            connection.InternalError += InternalError;
            connection.ConnectionRestored += ConnectionRestored;
            connection.ConfigurationChanged += ConfigurationChanged;
            connection.ConfigurationChangedBroadcast += ConfigurationChangedBroadcast;
            connection.HashSlotMoved += HashSlotMoved;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public void RegisterProfiler(Func<ProfilingSession> profilingSessionProvider)
        {
            connection.RegisterProfiler(profilingSessionProvider);
        }

        /// <inheritdoc />
        public ServerCounters GetCounters()
        {
            return connection.GetCounters();
        }

        /// <inheritdoc />
        public EndPoint[] GetEndPoints(bool configuredOnly = false)
        {
            return connection.GetEndPoints(configuredOnly);
        }

        /// <inheritdoc />
        public void Wait(Task task)
        {
            connection.Wait(task);
        }

        /// <inheritdoc />
        public T Wait<T>(Task<T> task)
        {
            return connection.Wait(task);
        }

        /// <inheritdoc />
        public void WaitAll(params Task[] tasks)
        {
            connection.WaitAll(tasks);
        }

        /// <inheritdoc />
        public int HashSlot(RedisKey key)
        {
            return connection.HashSlot(key);
        }

        /// <inheritdoc />
        public ISubscriber GetSubscriber(object asyncState = null)
        {
            return connection.GetSubscriber(asyncState);
        }

        /// <inheritdoc />
        public IDatabase GetDatabase(int db = -1, object asyncState = null)
        {
            return connection.GetDatabase(db, asyncState);
        }

        /// <inheritdoc />
        public IServer GetServer(string host, int port, object asyncState = null)
        {
            return connection.GetServer(host, port, asyncState);
        }

        /// <inheritdoc />
        public IServer GetServer(string hostAndPort, object asyncState = null)
        {
            return connection.GetServer(hostAndPort, asyncState);
        }

        /// <inheritdoc />
        public IServer GetServer(IPAddress host, int port)
        {
            return connection.GetServer(host, port);
        }

        /// <inheritdoc />
        public IServer GetServer(EndPoint endpoint, object asyncState = null)
        {
            return connection.GetServer(endpoint, asyncState);
        }

        /// <inheritdoc />
        public Task<bool> ConfigureAsync(TextWriter log = null)
        {
            return connection.ConfigureAsync(log);
        }

        /// <inheritdoc />
        public bool Configure(TextWriter log = null)
        {
            return connection.Configure(log);
        }

        /// <inheritdoc />
        public string GetStatus()
        {
            return connection.GetStatus();
        }

        /// <inheritdoc />
        public void GetStatus(TextWriter log)
        {
            connection.GetStatus(log);
        }

        /// <inheritdoc />
        public void Close(bool allowCommandsToComplete = true)
        {
            connection.Close(allowCommandsToComplete);
        }

        /// <inheritdoc />
        public Task CloseAsync(bool allowCommandsToComplete = true)
        {
            return connection.CloseAsync(allowCommandsToComplete);
        }

        /// <inheritdoc />
        public string GetStormLog()
        {
            return connection.GetStormLog();
        }

        /// <inheritdoc />
        public void ResetStormLog()
        {
            connection.ResetStormLog();
        }

        /// <inheritdoc />
        public long PublishReconfigure(CommandFlags flags = CommandFlags.None)
        {
            return connection.PublishReconfigure(flags);
        }

        /// <inheritdoc />
        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None)
        {
            return connection.PublishReconfigureAsync(flags);
        }

        /// <inheritdoc />
        public int GetHashSlot(RedisKey key)
        {
            return connection.GetHashSlot(key);
        }

        /// <inheritdoc />
        public void ExportConfiguration(Stream destination, ExportOptions options = ExportOptions.All)
        {
            connection.ExportConfiguration(destination, options);
        }

        /// <inheritdoc />
        public string ClientName => connection.ClientName;

        /// <inheritdoc />
        public string Configuration => connection.Configuration;

        /// <inheritdoc />
        public int TimeoutMilliseconds => connection.TimeoutMilliseconds;

        /// <inheritdoc />
        public long OperationCount => connection.OperationCount;

        /// <inheritdoc />
        public bool PreserveAsyncOrder
        {
#pragma warning disable 618
            get => connection.PreserveAsyncOrder;
            set => connection.PreserveAsyncOrder = value;
#pragma warning restore 618
        }

        /// <inheritdoc />
        public bool IsConnected => connection.IsConnected;

        /// <inheritdoc />
        public bool IsConnecting => connection.IsConnecting;

        /// <inheritdoc />
        public bool IncludeDetailInExceptions
        {
            get => connection.IncludeDetailInExceptions;
            set => connection.IncludeDetailInExceptions = value;
        }

        /// <inheritdoc />
        public int StormLogThreshold
        {
            get => connection.StormLogThreshold;
            set => connection.StormLogThreshold = value;
        }

        /// <inheritdoc />
        public event EventHandler<RedisErrorEventArgs> ErrorMessage;

        /// <inheritdoc />
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;

        /// <inheritdoc />
        public event EventHandler<InternalErrorEventArgs> InternalError;

        /// <inheritdoc />
        public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored;

        /// <inheritdoc />
        public event EventHandler<EndPointEventArgs> ConfigurationChanged;

        /// <inheritdoc />
        public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast;

        /// <inheritdoc />
        public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved;
    }

    public class RedisConnectionPool : IAsyncDisposable
    {
        private readonly List<IConnectionMultiplexer> connectionMultiplexers 
            = new List<IConnectionMultiplexer>();

        private readonly string connectionString;
        private readonly int connectionCount;

        public RedisConnectionPool(string connectionString, int count = 10)
        {
            this.connectionString = connectionString;
            this.connectionCount = count;
        }

        public IConnectionMultiplexer Get()
        {
            if(connectionMultiplexers.Count != connectionCount)
            {
                var connection = new RedisConnection(
                    ConnectionMultiplexer.Connect(connectionString));
                connectionMultiplexers.Add(connection);
                return connection;
            }
            return connectionMultiplexers.OrderBy(x => x.OperationCount).FirstOrDefault();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            foreach(var c in connectionMultiplexers)
            {
                c.Dispose();
            }

            return default;
        }
    }
}

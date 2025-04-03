// <copyright file="InMemoryStreamStoreProvider{TData}.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Commons.Configurations;
using Hexalith.Commons.UniqueIds;
using Hexalith.Streams.Configurations;
using Hexalith.Streams.Exceptions;
using Hexalith.Streams.Providers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// In-memory implementation of the stream store provider.
/// </summary>
/// <typeparam name="TData">The type of the data stored in the stream.</typeparam>
public sealed partial class InMemoryStreamStoreProvider<TData> : IStreamStoreProvider<TData>
{
    private readonly Dictionary<string, Dictionary<string, long>> _idempotencyIds = [];
    private readonly System.Threading.Lock _lock = new();
    private readonly TimeSpan _lockTimeout;
    private readonly ILogger<InMemoryStreamStoreProvider<TData>> _logger;
    private readonly Dictionary<string, FileStreamStoreLock> _sessions = [];
    private readonly Dictionary<string, Dictionary<long, IStreamStoreObject<TData>>> _snapshots = [];
    private readonly Dictionary<string, Dictionary<long, IStreamStoreObject<TData>>> _streams = [];
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryStreamStoreProvider{TData}"/> class.
    /// </summary>
    /// <param name="options">The options for the stream store provider.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryStreamStoreProvider(
            IOptions<StreamsSettings> options,
            TimeProvider timeProvider,
            ILogger<InMemoryStreamStoreProvider<TData>> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        SettingsException<StreamsSettings>.ThrowIfUndefined(options.Value.LockTimeout);
        _timeProvider = timeProvider;
        _logger = logger;
        _lockTimeout = options.Value.LockTimeout!.Value!;
    }

    /// <inheritdoc/>
    public Task CloseSessionAsync([NotNull] string sessionId, [NotNull] string streamId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        using (_lock.EnterScope())
        {
            if (!_sessions.Remove(sessionId, out _))
            {
                // Log an error if the session was not found
                _logger.LogError("Close session : Session '{SessionId}' not found for stream '{StreamId}'.", sessionId, streamId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IStreamStoreObject<TData>> GetAsync([NotNull] string sessionId, [NotNull] string streamId, long version, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);

        if (_streams.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? stream) &&
            stream.TryGetValue(version, out IStreamStoreObject<TData>? value))
        {
            return Task.FromResult(value);
        }

        throw new StreamVersionNotFoundException(streamId, version);
    }

    /// <inheritdoc/>
    public Task<IStreamStoreObject<TData>> GetAsync([NotNull] string sessionId, [NotNull] string streamId, [NotNull] string idempotencyId, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        if (_idempotencyIds.TryGetValue(streamId, out Dictionary<string, long>? idempotencyIds) &&
            idempotencyIds.TryGetValue(idempotencyId, out long version))
        {
            return GetAsync(sessionId, streamId, version, cancellationToken);
        }

        throw new StreamIdempotencyIdNotFoundException(streamId, streamId);
    }

    /// <inheritdoc/>
    public Task<IStreamStoreObject<TData>> GetSnapshotAsync([NotNull] string sessionId, [NotNull] string streamId, long version, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        if (_snapshots.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? snapshots) &&
            snapshots.TryGetValue(version, out IStreamStoreObject<TData>? value))
        {
            return Task.FromResult(value);
        }

        throw new StreamVersionNotFoundException(streamId, version);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<long>> GetSnapshotVersionsAsync([NotNull] string sessionId, [NotNull] string streamId, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        if (_snapshots.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? snapshots))
        {
            return Task.FromResult<IEnumerable<long>>([.. snapshots.Keys]);
        }

        return Task.FromResult<IEnumerable<long>>([]);
    }

    /// <inheritdoc/>
    public Task<long> GetVersionAsync([NotNull] string sessionId, [NotNull] string streamId, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        if (_streams.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? stream)
            && stream.Count > 0)
        {
            return Task.FromResult(stream.Keys.Max());
        }

        return Task.FromResult(0L);
    }

    /// <inheritdoc/>
    public Task<string> OpenSessionAsync([NotNull] string streamId, CancellationToken cancellationToken) => OpenSessionAsync(streamId, _lockTimeout, cancellationToken);

    /// <inheritdoc/>
    public Task<string> OpenSessionAsync([NotNull] string streamId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        do
        {
            using (_lock.EnterScope())
            {
                // Check if the stream is already locked
                if (_sessions.TryGetValue(streamId, out FileStreamStoreLock? streamLock) && streamLock.Expiration > _timeProvider.GetUtcNow())
                {
                    // If the stream is locked, wait for the lock to be released
                    Thread.Sleep(50);
                    continue;
                }

                // Create a new lock for the stream
                var newLock = new FileStreamStoreLock(
                    UniqueIdHelper.GenerateUniqueStringId(),
                    _timeProvider.GetUtcNow().Add(timeout));

                // Try to add the lock to the dictionary
                _sessions[streamId] = newLock;
                return Task.FromResult(newLock.SessionId);
            }
        }
        while (stopwatch.Elapsed < timeout);

        return Task.FromException<string>(
            new TimeoutException(
                $"Could not open a new session for stream '{streamId}'. Close the previous session or increase the lock timeout."));
    }

    /// <inheritdoc/>
    public Task RemoveSnapshotAsync([NotNull] string sessionId, [NotNull] string streamId, long version, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        if (!_snapshots.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? snapshots)
            || !snapshots.Remove(version, out _))
        {
            // Log an error if the version was not found
            _logger.LogError("Remove snapshot : Version '{Version}' not found for stream '{StreamId}'.", version, streamId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetAsync(
        [NotNull] string sessionId,
        [NotNull] string streamId,
        [NotNull] string idempotencyId,
        TData value,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyId);
        long version = 1;
        ValidateSession(sessionId, streamId);
        using (_lock.EnterScope())
        {
            Dictionary<string, long> idempotencies = GetIdempotencies(streamId);
            if (idempotencies.TryGetValue(idempotencyId, out long existingVersion))
            {
                return Task.FromException(new DuplicateStreamItemIdempotencyIdException(streamId, idempotencyId, existingVersion));
            }

            Dictionary<long, IStreamStoreObject<TData>> stream = GetStream(streamId, ref version);

            stream.Add(++version, new StreamStoreObject<TData>(idempotencyId, version, value));

            // Add the idempotency ID to the stream
            idempotencies.Add(idempotencyId, version);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetAsync(
        [NotNull] string sessionId,
        [NotNull] string streamId,
        IStreamStoreObject<TData> value,
        CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        using (_lock.EnterScope())
        {
            long version = 0;

            Dictionary<string, long> idempotencies = GetIdempotencies(streamId);
            if (idempotencies.TryGetValue(value.IdempotencyId, out long existingVersion))
            {
                return Task.FromException(new DuplicateStreamItemIdempotencyIdException(streamId, value.IdempotencyId, existingVersion));
            }

            Dictionary<long, IStreamStoreObject<TData>> stream = GetStream(streamId, ref version);
            if (value.Version != ++version)
            {
                throw new StreamVersionMismatchException(streamId, value.Version, version + 1);
            }

            stream.Add(version, value);

            // Add the idempotency ID to the stream
            idempotencies.Add(value.IdempotencyId, version);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetSnapshotAsync([NotNull] string sessionId, [NotNull] string streamId, IStreamStoreObject<TData> value, CancellationToken cancellationToken)
    {
        ValidateSession(sessionId, streamId);
        Dictionary<long, IStreamStoreObject<TData>> snapshots = GetSnapshots(streamId);
        snapshots[value.Version] = value;
        return Task.CompletedTask;
    }

    private Dictionary<string, long> GetIdempotencies(string streamId)
    {
        if (!_idempotencyIds.TryGetValue(streamId, out Dictionary<string, long>? streamIdempotency))
        {
            // Create a new stream if it doesn't exist
            streamIdempotency = [];
            _idempotencyIds[streamId] = streamIdempotency;
        }

        return streamIdempotency;
    }

    private Dictionary<long, IStreamStoreObject<TData>> GetSnapshots(string streamId)
    {
        if (!_snapshots.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? snapshots))
        {
            // Create a new stream if it doesn't exist
            snapshots = [];
            _snapshots[streamId] = snapshots;
        }

        return snapshots;
    }

    private Dictionary<long, IStreamStoreObject<TData>> GetStream(string streamId, ref long version)
    {
        // Find latest version in the stream
        if (_streams.TryGetValue(streamId, out Dictionary<long, IStreamStoreObject<TData>>? stream))
        {
            version = stream.Keys.Max();
        }
        else
        {
            // Create a new stream if it doesn't exist
            stream = [];
            _streams[streamId] = stream;
        }

        return stream;
    }

    private void ValidateSession([NotNull] string sessionId, [NotNull] string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(streamId, out FileStreamStoreLock? streamLock) || streamLock.SessionId != sessionId)
        {
            throw new InvalidOperationException($"Session '{sessionId}' is not valid for stream '{streamId}'.");
        }
    }
}
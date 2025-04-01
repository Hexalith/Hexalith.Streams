// <copyright file="InMemoryStream{TData}.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// In-memory implementation of a stream.
/// </summary>
/// <typeparam name="TData">The type of the data in the stream.</typeparam>
/// <param name="streamId">The stream identifier.</param>
internal sealed class InMemoryStream<TData>(string streamId) : IStoreStream<TData>
{
    private readonly List<IStreamStoreObject<TData>> _items = [];
    private readonly Lock _lock = new();
    private IStreamStoreObject<TData>? _snapshot;
    private long _snapshotVersion;

    /// <summary>
    /// Add new items to the stream.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new stream version.</returns>
    public Task<long> AddAsync(IEnumerable<IStreamStoreObject<TData>> items, CancellationToken cancellationToken)
    {
        using (_lock.EnterScope())
        {
            _items.AddRange(items);
        }

        return Task.FromResult((long)_items.Count);
    }

    /// <summary>
    /// Add new items to the stream and verify the version.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="expectedVersion">The expected stream version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new stream version.</returns>
    /// <exception cref="InvalidOperationException">When the current version doesn't match the expected version.</exception>
    public async Task<long> AddAsync(IEnumerable<IStreamStoreObject<TData>> items, long expectedVersion, CancellationToken cancellationToken)
    {
        if (_items.Count != expectedVersion)
        {
            throw new InvalidOperationException($"Stream '{streamId}' expected version {expectedVersion} but was {_items.Count}");
        }

        return await AddAsync(items, cancellationToken);
    }

    /// <summary>
    /// Clear the snapshot of the stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    public Task ClearSnapshotAsync(CancellationToken cancellationToken)
    {
        using (_lock.EnterScope())
        {
            _snapshot = null;
            _snapshotVersion = 0;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a stream items slice.
    /// </summary>
    /// <param name="first">first item to retrieve.</param>
    /// <param name="last">last item to retrieve.</param>
    /// <param name="useSnapshot">if set to <c>true</c> use a snapshot to avoid replaying all events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stream data slice.</returns>
    public Task<IEnumerable<IStreamStoreObject<TData>>> GetAsync(long first, long last, bool useSnapshot, CancellationToken cancellationToken)
    {
        using (_lock.EnterScope())
        {
            if (first < 0 || last < first || last >= _items.Count)
            {
                return Task.FromResult(Enumerable.Empty<IStreamStoreObject<TData>>());
            }

            IEnumerable<IStreamStoreObject<TData>> result;

            if (useSnapshot && _snapshot != null && first <= _snapshotVersion)
            {
                if (_snapshotVersion >= last)
                {
                    // All items are covered by the snapshot
                    result = [_snapshot];
                }
                else
                {
                    // Combine snapshot with items after the snapshot
                    IEnumerable<IStreamStoreObject<TData>> itemsAfterSnapshot = _items
                        .Skip((int)(_snapshotVersion + 1))
                        .Take((int)(last - _snapshotVersion));

                    result = new[] { _snapshot }.Concat(itemsAfterSnapshot);
                }
            }
            else
            {
                // No snapshot or not using it
                result = _items
                    .Skip((int)first)
                    .Take((int)(last - first + 1));
            }

            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Gets all stream items.
    /// </summary>
    /// <param name="useSnapshot">if set to <c>true</c> use a snapshot to avoid replaying all events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of all items.</returns>
    public async Task<StreamStoreResult<TData>> GetAsync(bool useSnapshot, CancellationToken cancellationToken)
    {
        int version = _items.Count;
        IEnumerable<IStreamStoreObject<TData>> result = await GetAsync(0, version, useSnapshot, cancellationToken);
        return new StreamStoreResult<TData>(result, version);
    }

    /// <summary>
    /// Take a snapshot of the stream. Sets a snapshot of the stream to avoid replaying all events. The application is responsible for managing the snapshot.
    /// </summary>
    /// <param name="version">The version of the stream of the snapshot.</param>
    /// <param name="snapshot">The calculated state of the stream at the given version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    /// <exception cref="ArgumentException">When the snapshot version is invalid.</exception>
    public Task SnapshotAsync(long version, IStreamStoreObject<TData> snapshot, CancellationToken cancellationToken)
    {
        if (version < 0 || version >= _items.Count)
        {
            return Task.FromException(new ArgumentException($"Invalid snapshot version {version} for stream '{streamId}' with {_items.Count} items", nameof(version)));
        }

        using (_lock.EnterScope())
        {
            _snapshot = snapshot;
            _snapshotVersion = version;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the stream version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns the current version of the stream.</returns>
    public Task<long> VersionAsync(CancellationToken cancellationToken)
        => Task.FromResult((long)_items.Count);
}
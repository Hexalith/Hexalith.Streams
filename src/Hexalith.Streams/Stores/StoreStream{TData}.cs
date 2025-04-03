// <copyright file="StoreStream{TData}.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Streams.Providers;

/// <summary>
/// In-memory implementation of a stream.
/// </summary>
/// <typeparam name="TData">The type of the data in the stream.</typeparam>
/// <param name="streamId">The stream identifier.</param>
/// <param name="provider">The stream store provider.</param>
public sealed class StoreStream<TData>(string streamId, IStreamStoreProvider<TData> provider) : IStoreStream<TData>
{
    private string? _sessionId;

    /// <summary>
    /// Add new items to the stream.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new stream version.</returns>
    public async Task<long> AddAsync(IEnumerable<IStreamStoreData<TData>> items, CancellationToken cancellationToken)
    {
        long version = await provider.GetVersionAsync(await GetSessionIdAsync(cancellationToken), streamId, cancellationToken);

        foreach (IStreamStoreObject<TData> item in items.OfType<IStreamStoreObject<TData>>())
        {
            await provider.SetAsync(streamId, item, cancellationToken);
            version++;
        }

        return version;
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
        long version = await provider.GetVersionAsync(streamId, cancellationToken);
        if (version != expectedVersion)
        {
            throw new InvalidVersionException(streamId, expectedVersion, version);
        }

        foreach (IStreamStoreObject<TData> item in items)
        {
            await provider.SetAsync(streamId, item, cancellationToken);
            version++;
        }

        return version;
    }

    /// <inheritdoc/>
    public Task<long> AddAsync(IEnumerable<IStreamStoreObject<TData>> items, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ClearSnapshotAsync(long version, CancellationToken cancellationToken)
        => provider.RemoveSnapshotAsync(streamId, version, cancellationToken);

    /// <summary>
    /// Clear the snapshot of the stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    public async Task ClearSnapshotsAsync(CancellationToken cancellationToken)
    {
        IEnumerable<long> versions = await provider.GetSnapshotVersionsAsync(streamId, cancellationToken);
        List<Task> tasks = [];
        foreach (long version in versions)
        {
            tasks.Add(provider.RemoveSnapshotAsync(streamId, version, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => throw new NotImplementedException();

    /// <summary>
    /// Gets all stream items.
    /// </summary>
    /// <param name="version">The version of the stream to retrieve.</param>
    /// <param name="useSnapshot">if set to <c>true</c> use a snapshot to avoid replaying all events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of all items.</returns>
    public async Task<IEnumerable<IStreamStoreObject<TData>>> GetAsync(long version, bool useSnapshot, CancellationToken cancellationToken)
    {
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than 0");
        }

        long first = 1L;
        if (useSnapshot)
        {
            long snapshot = (await provider.GetSnapshotVersionsAsync(streamId, cancellationToken))
                .Where(p => p <= version)
                .OrderByDescending(p => p)
                .FirstOrDefault();
            if (snapshot > 0)
            {
                first = snapshot;
            }
        }

        return await GetSliceAsync(first, version, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IStreamStoreObject<TData>>> GetAsync(bool useSnapshot, CancellationToken cancellationToken)
    {
        long version = await provider.GetVersionAsync(streamId, cancellationToken);
        if (version < 1)
        {
            return [];
        }

        return await GetAsync(version, useSnapshot, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IStreamStoreObject<TData>>> GetAsync(CancellationToken cancellationToken) => await GetAsync(true, cancellationToken);

    /// <summary>
    /// Gets a stream items slice.
    /// </summary>
    /// <param name="first">first item to retrieve.</param>
    /// <param name="last">last item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stream data slice.</returns>
    public async Task<IEnumerable<IStreamStoreObject<TData>>> GetSliceAsync(long first, long last, CancellationToken cancellationToken)
    {
        if (first < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(first), "First item must be greater than 0");
        }

        if (last < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(last), "Last item must be greater than 0");
        }

        if (last < first)
        {
            throw new ArgumentOutOfRangeException(nameof(last), "Last item must be greater than or equal to first item");
        }

        List<Task<IStreamStoreObject<TData>>> tasks = [];
        for (long i = first; i <= last; i++)
        {
            tasks.Add(provider.GetAsync(streamId, i, cancellationToken));
        }

        return await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task<long> LastSnapshotVersionAsync(CancellationToken cancellationToken)
        => (await provider.GetSnapshotVersionsAsync(streamId, cancellationToken))
            .OrderByDescending(p => p).FirstOrDefault();

    /// <inheritdoc/>
    public async Task SnapshotAllAsync(int chunkSize, Func<IEnumerable<IStreamStoreObject<TData>>, IStreamStoreObject<TData>> snapshot, CancellationToken cancellationToken)
    {
        await ClearSnapshotsAsync(cancellationToken);
        long version = await provider.GetVersionAsync(streamId, cancellationToken);
        if (version < chunkSize)
        {
            return;
        }

        long chunks = version / chunkSize;
        for (int i = 0; i < chunks; i++)
        {
            IEnumerable<IStreamStoreObject<TData>> items = await GetAsync(i * chunkSize, true, cancellationToken);
            IStreamStoreObject<TData> snap = snapshot(items);
            await provider.SetSnapshotAsync(streamId, snap, cancellationToken);
        }
    }

    /// <summary>
    /// Take a snapshot of the stream. Sets a snapshot of the stream to avoid replaying all events. The application is responsible for managing the snapshot.
    /// </summary>
    /// <param name="snapshot">The calculated state of the stream at the given version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    /// <exception cref="ArgumentException">When the snapshot version is invalid.</exception>
    public async Task SnapshotAsync(IStreamStoreObject<TData> snapshot, CancellationToken cancellationToken) => await provider.SetSnapshotAsync(streamId, snapshot, cancellationToken);

    /// <inheritdoc/>
    public Task SnapshotAsync(long version, IStreamStoreObject<TData> snapshot, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IEnumerable<long>> SnapshotVersionsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <summary>
    /// Gets the stream version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns the current version of the stream.</returns>
    public async Task<long> VersionAsync(CancellationToken cancellationToken)
        => await provider.GetVersionAsync(streamId, cancellationToken);

    protected async Task<string> GetSessionIdAsync(CancellationToken cancellationToken)
    {
        _sessionId ??= await provider.OpenSessionAsync(streamId, cancellationToken);
        return _sessionId;
    }
}
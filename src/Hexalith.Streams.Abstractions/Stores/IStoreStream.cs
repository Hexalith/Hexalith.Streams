// <copyright file="IStoreStream.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

/// <summary>
/// Persisted stream interface.
/// </summary>
/// <typeparam name="TData">The type of the data in the stream.</typeparam>
public interface IStoreStream<TData>
{
    /// <summary>
    /// Add new items to the stream.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new stream version.</returns>
    Task<long> AddAsync(IEnumerable<IStreamStoreObject<TData>> items, CancellationToken cancellationToken);

    /// <summary>
    /// Add new items to the stream and verify the version.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="expectedVersion">The expected stream version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new stream version.</returns>
    Task<long> AddAsync(IEnumerable<IStreamStoreObject<TData>> items, long expectedVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Clear the snapshot of the stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task ClearSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a stream items slice.
    /// </summary>
    /// <param name="first">first item to retrieve.</param>
    /// <param name="last">last item to retrieve.</param>
    /// <param name="useSnapshot">if set to <c>true</c> use a snapshot to avoid replaying all events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stream data slice.</returns>
    Task<IEnumerable<IStreamStoreObject<TData>>> GetAsync(long first, long last, bool useSnapshot, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all stream items.
    /// </summary>
    /// <param name="useSnapshot">if set to <c>true</c> use a snapshot to avoid replaying all events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of all items.</returns>
    Task<StreamStoreResult<TData>> GetAsync(bool useSnapshot, CancellationToken cancellationToken);

    /// <summary>
    /// Take a snapshot of the stream. Sets a snapshot of the stream to avoid replaying all events. The application is responsible for managing the snapshot.
    /// </summary>
    /// <param name="version">The version of the stream of the snapshot.</param>
    /// <param name="snapshot">The calculated state of the stream at the given version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task SnapshotAsync(long version, IStreamStoreObject<TData> snapshot, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the stream version of the snapshot.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns the snapshot version.</returns>
    Task<long> SnapshotVersionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the stream version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns the current version of the stream.</returns>
    Task<long> VersionAsync(CancellationToken cancellationToken);
}
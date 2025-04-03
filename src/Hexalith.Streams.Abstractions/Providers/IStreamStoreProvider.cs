// <copyright file="IStreamStoreProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Providers;

using System.Threading.Tasks;

using Hexalith.Streams.Stores;

/// <summary>
/// Interface for providing stream data.
/// </summary>
/// <typeparam name="TData">The type of data in the stream.</typeparam>
public interface IStreamStoreProvider<TData>
{
    /// <summary>
    /// Closes the session for the specified stream.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CloseSessionAsync(string sessionId, string streamId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the stream data asynchronously by stream ID and version.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version of the stream data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the stream store object.</returns>
    Task<IStreamStoreObject<TData>> GetAsync(string sessionId, string streamId, long version, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the stream data asynchronously by stream ID and idempotency ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="idempotencyId">The idempotency identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the stream store object.</returns>
    Task<IStreamStoreObject<TData>> GetAsync(string sessionId, string streamId, string idempotencyId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the snapshot data asynchronously by stream ID and version.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version of the snapshot data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the stream store object.</returns>
    Task<IStreamStoreObject<TData>> GetSnapshotAsync(string sessionId, string streamId, long version, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the snapshot versions asynchronously by stream ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of snapshot versions.</returns>
    Task<IEnumerable<long>> GetSnapshotVersionsAsync(string sessionId, string streamId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the version of the stream data asynchronously by stream ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the version of the stream data.</returns>
    Task<long> GetVersionAsync(string sessionId, string streamId, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a session for the specified stream using the default timeout defined in the application configuration.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The session identifier.</returns>
    Task<string> OpenSessionAsync(string streamId, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a session for the specified stream with a timeout.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="timeout">The timeout duration for the session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The session identifier.</returns>
    Task<string> OpenSessionAsync(string streamId, TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the snapshot data asynchronously by stream ID and version.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version of the snapshot data to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveSnapshotAsync(string sessionId, string streamId, long version, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the stream data asynchronously.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="value">The value to set in the stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync(string sessionId, string streamId, IStreamStoreObject<TData> value, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the stream data asynchronously with idempotency.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="idempotencyId">The idempotency identifier.</param>
    /// <param name="value">The value to set in the stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync(string sessionId, string streamId, string idempotencyId, TData value, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the snapshot data asynchronously.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="value">The value to set in the snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetSnapshotAsync(string sessionId, string streamId, IStreamStoreObject<TData> value, CancellationToken cancellationToken);
}
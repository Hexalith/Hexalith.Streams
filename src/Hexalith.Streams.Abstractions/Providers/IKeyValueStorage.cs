// <copyright file="IKeyValueStorage.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Providers;

/// <summary>
/// Interface for key-value storage operations.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public interface IKeyValueStorage<TValue>
{
    /// <summary>
    /// Deletes the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation. The task result contains a boolean indicating whether the delete was successful.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a value exists for the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="value">The value to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous exist operation. The task result contains a boolean indicating whether the value exists.</returns>
    Task<bool> ExistAsync(string key, TValue value, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous get operation. The task result contains the storage result with the value.</returns>
    Task<StorageResult<TValue>> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to set.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    Task SetAsync(string key, TValue value, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="defaultValue">The default value to return if the key does not exist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous try get operation. The task result contains the value or the default value if the key does not exist.</returns>
    Task<TValue> TryGetAsync(string key, TValue defaultValue, CancellationToken cancellationToken);
}
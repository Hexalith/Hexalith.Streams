// <copyright file="IStreamStoreData.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

/// <summary>
/// Persisted stream item interface.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
public interface IStreamStoreData<out TData>
{
    /// <summary>
    /// Gets the data object.
    /// </summary>
    /// <value>The data.</value>
    TData Data { get; }

    /// <summary>
    /// Gets the idempotency identifier used to ensure that the same data is not processed twice.
    /// </summary>
    string IdempotencyId { get; }
}
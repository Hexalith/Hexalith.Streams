// <copyright file="IStreamResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Abstractions.Stores;

/// <summary>
/// Persisted stream item interface.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
public interface IStreamResult<TData>
{
    /// <summary>
    /// Gets the message.
    /// </summary>
    IEnumerable<IStreamObject<TData>> Objects { get; }

    /// <summary>
    /// Gets the stream sequence number.
    /// </summary>
    long Version { get; }
}
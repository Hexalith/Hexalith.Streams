// <copyright file="InMemoryStreamStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System.Collections.Concurrent;

/// <summary>
/// In-memory implementation of the stream store.
/// </summary>
/// <typeparam name="TData">The type of the data stored in the stream.</typeparam>
public sealed class InMemoryStreamStore<TData> : IStreamStore<TData>
{
    private readonly ConcurrentDictionary<string, InMemoryStream<TData>> _streams = new();

    /// <summary>
    /// Gets an event stream. If the stream does not exist, it is created.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>The event stream.</returns>
    public IStoreStream<TData> GetStream(string streamId)
        => _streams.GetOrAdd(streamId, id => new InMemoryStream<TData>(id));
}
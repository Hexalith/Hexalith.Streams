// <copyright file="IStreamStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Abstractions.Stores;

/// <summary>
/// Event store interface.
/// </summary>
/// <typeparam name="TData">The type of the data stored in the stream.</typeparam>
public interface IStreamStore<TData>
    where TData : class
{
    /// <summary>
    /// Gets an event stream. If the stream does not exist, it is created.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>The event stream.</returns>
    IObjectStream<TData> GetStream(string streamId);
}
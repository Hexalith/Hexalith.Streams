﻿// <copyright file="IStreamStoreObject.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

/// <summary>
/// Persisted stream item interface.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
public interface IStreamStoreObject<out TData> : IStreamStoreData<TData>
{
    /// <summary>
    /// Gets the stream item version.
    /// </summary>
    long Version { get; }
}
// <copyright file="StreamStoreResult{TData}.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System.Collections.Generic;

/// <summary>
/// Represents the result of a stream store operation.
/// </summary>
/// <typeparam name="TData">The type of the data in the stream.</typeparam>
/// <param name="Objects">The stream objects.</param>
/// <param name="Version">The stream version.</param>
public sealed record StreamStoreResult<TData>(
    IEnumerable<IStreamStoreObject<TData>> Objects,
    long Version);
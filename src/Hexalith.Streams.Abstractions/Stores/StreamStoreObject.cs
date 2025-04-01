// <copyright file="StreamStoreObject.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;
/// <summary>
/// Represents a stream store object.
/// </summary>
/// <typeparam name="TData">The type of the data in the stream.</typeparam>
/// <param name="Data">The data object.</param>
/// <param name="IdempotencyId">The idempotency identifier used to ensure that the same data is not processed twice.</param>
public sealed record StreamStoreObject<TData>(
    TData Data,
    string IdempotencyId) : IStreamStoreObject<TData>;
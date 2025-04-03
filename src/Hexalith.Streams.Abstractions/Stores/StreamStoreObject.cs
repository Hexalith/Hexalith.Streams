// <copyright file="StreamStoreObject.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System.Runtime.Serialization;

/// <summary>
/// Represents an object in a stream store.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
/// <param name="IdempotencyId">The idempotency identifier used to ensure that the same data is not processed twice.</param>
/// <param name="Version">The version of the object in the stream.</param>
/// <param name="Data">The data object.</param>
[DataContract]
public sealed record StreamStoreObject<TData>(
    [property: DataMember(Order = 1)] string IdempotencyId,
    [property: DataMember(Order = 2)] long Version,
    [property: DataMember(Order = 3)] TData Data
    ) : IStreamStoreObject<TData>;
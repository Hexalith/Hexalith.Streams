// <copyright file="IStreamStoreSerializer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Providers;

using Hexalith.Streams.Stores;

/// <summary>
/// Interface for serializing and deserializing stream store objects.
/// </summary>
/// <typeparam name="TData">The type of the data contained in the stream store object.</typeparam>
public interface IStreamStoreSerializer<TData>
{
    /// <summary>
    /// Gets the type of serialization used.
    /// </summary>
    string SerializationType { get; }

    /// <summary>
    /// Deserializes the specified byte array to a stream store object.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <returns>The deserialized stream store object.</returns>
    IStreamStoreObject<TData> Deserialize(byte[] data);

    /// <summary>
    /// Deserializes the specified byte array to a stream store object asynchronously.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized stream store object.</returns>
    Task<IStreamStoreObject<TData>> DeserializeAsync(byte[] data, CancellationToken cancellationToken) => Task.FromResult(Deserialize(data));

    /// <summary>
    /// Deserializes the specified stream to a stream store object asynchronously.
    /// </summary>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized stream store object.</returns>
    Task<IStreamStoreObject<TData>> DeserializeAsync(Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Serializes the specified stream store object to a byte array.
    /// </summary>
    /// <param name="data">The stream store object to serialize.</param>
    /// <returns>The serialized byte array.</returns>
    byte[] Serialize(IStreamStoreObject<TData> data);

    /// <summary>
    /// Serializes the specified stream store object to a byte array asynchronously.
    /// </summary>
    /// <param name="data">The stream store object to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the serialized byte array.</returns>
    Task<byte[]> SerializeAsync(IStreamStoreObject<TData> data, CancellationToken cancellationToken) => Task.FromResult(Serialize(data));

    /// <summary>
    /// Serializes the specified stream store object to a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to serialize to.</param>
    /// <param name="data">The stream store object to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SerializeAsync(Stream stream, IStreamStoreObject<TData> data, CancellationToken cancellationToken);
}
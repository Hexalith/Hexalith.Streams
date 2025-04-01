// <copyright file="InMemoryStreamStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Tests.Stores;

using Hexalith.Streams.Stores;

using Shouldly;

/// <summary>
/// Tests for <see cref="InMemoryStreamStore{TData}"/>.
/// </summary>
public class InMemoryStreamStoreTests
{
    /// <summary>
    /// Tests that GetStream returns different stream instances for different stream IDs.
    /// </summary>
    [Fact]
    public void GetStream_ShouldReturnDifferentInstancesForDifferentIds()
    {
        // Arrange
        var streamStore = new InMemoryStreamStore<string>();
        const string streamId1 = "test-stream-1";
        const string streamId2 = "test-stream-2";

        // Act
        IStoreStream<string> stream1 = streamStore.GetStream(streamId1);
        IStoreStream<string> stream2 = streamStore.GetStream(streamId2);

        // Assert
        stream1.ShouldNotBe(stream2);
    }

    /// <summary>
    /// Tests that GetStream returns the same stream instance for the same stream ID.
    /// </summary>
    [Fact]
    public void GetStream_ShouldReturnSameInstanceForSameId()
    {
        // Arrange
        var streamStore = new InMemoryStreamStore<string>();
        const string streamId = "test-stream";

        // Act
        IStoreStream<string> stream1 = streamStore.GetStream(streamId);
        IStoreStream<string> stream2 = streamStore.GetStream(streamId);

        // Assert
        stream1.ShouldBe(stream2);
    }

    /// <summary>
    /// Tests that GetStream returns a valid stream.
    /// </summary>
    [Fact]
    public void GetStream_ShouldReturnValidStream()
    {
        // Arrange
        var streamStore = new InMemoryStreamStore<string>();
        const string streamId = "test-stream";

        // Act
        IStoreStream<string> stream = streamStore.GetStream(streamId);

        // Assert
        _ = stream.ShouldNotBeNull();
        _ = stream.ShouldBeOfType<IStoreStream<string>>();
    }
}
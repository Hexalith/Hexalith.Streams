// <copyright file="InMemoryStreamTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Tests.Stores;

using Hexalith.Streams.Stores;

using Shouldly;

/// <summary>
/// Tests for <see cref="InMemoryStream{TData}"/> accessed through <see cref="IStoreStream{TData}"/> interface.
/// </summary>
public class InMemoryStreamTests
{
    /// <summary>
    /// Tests that adding items increases version.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task AddItems_ShouldIncreaseVersion()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
        ];

        // Act
        long newVersion = await stream.AddAsync(items, CancellationToken.None);
        long currentVersion = await stream.VersionAsync(CancellationToken.None);

        // Assert
        newVersion.ShouldBe(2);
        currentVersion.ShouldBe(2);
    }

    /// <summary>
    /// Tests that adding items with expected version succeeds when version matches.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task AddItemsWithExpectedVersion_ShouldSucceedWhenVersionMatches()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
        ];

        // Act
        long newVersion = await stream.AddAsync(items, 0, CancellationToken.None);

        // Assert
        newVersion.ShouldBe(1);
    }

    /// <summary>
    /// Tests that adding items with expected version throws when version doesn't match.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task AddItemsWithExpectedVersion_ShouldThrowWhenVersionDoesntMatch()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
        ];

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await stream.AddAsync(items, 1, CancellationToken.None));
    }

    /// <summary>
    /// Tests that clearing snapshot works correctly.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task ClearSnapshot_ShouldWork()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
            new StreamStoreObject<string>("data3", "idempotency3"),
        ];
        _ = await stream.AddAsync(items, CancellationToken.None);

        // Create a snapshot at version 2
        var snapshot = new StreamStoreObject<string>("snapshot-data", "snapshot-id");
        await stream.SnapshotAsync(2, snapshot, CancellationToken.None);

        // Act - Clear the snapshot and get data
        await stream.ClearSnapshotAsync(CancellationToken.None);
        StreamStoreResult<string> result = await stream.GetAsync(true, CancellationToken.None);

        // Assert
        result.Version.ShouldBe(3);
        IStreamStoreObject<string>[] resultArray = [.. result.Objects];
        resultArray.Length.ShouldBe(3); // Should get all items since snapshot is cleared
        resultArray[0].Data.ShouldBe("data1");
        resultArray[1].Data.ShouldBe("data2");
        resultArray[2].Data.ShouldBe("data3");
    }

    /// <summary>
    /// Tests that retrieving items returns correct data.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task GetItems_ShouldReturnCorrectData()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
            new StreamStoreObject<string>("data3", "idempotency3"),
        ];
        _ = await stream.AddAsync(items, CancellationToken.None);

        // Act
        StreamStoreResult<string> result = await stream.GetAsync(false, CancellationToken.None);

        // Assert
        result.Version.ShouldBe(3);
        result.Objects.Count().ShouldBe(3);
        result.Objects.ElementAt(0).Data.ShouldBe("data1");
        result.Objects.ElementAt(1).Data.ShouldBe("data2");
        result.Objects.ElementAt(2).Data.ShouldBe("data3");
    }

    /// <summary>
    /// Tests that retrieving a slice of items returns correct data.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task GetItemsSlice_ShouldReturnCorrectData()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
            new StreamStoreObject<string>("data3", "idempotency3"),
            new StreamStoreObject<string>("data4", "idempotency4"),
        ];
        _ = await stream.AddAsync(items, CancellationToken.None);

        // Act
        IEnumerable<IStreamStoreObject<string>> result = await stream.GetAsync(1, 2, false, CancellationToken.None);

        // Assert
        IStreamStoreObject<string>[] resultArray = [.. result];
        resultArray.Length.ShouldBe(2);
        resultArray[0].Data.ShouldBe("data2");
        resultArray[1].Data.ShouldBe("data3");
    }

    /// <summary>
    /// Tests that a new stream has version 0.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task NewStream_ShouldHaveVersionZero()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");

        // Act
        long version = await stream.VersionAsync(CancellationToken.None);

        // Assert
        version.ShouldBe(0);
    }

    /// <summary>
    /// Tests that snapshot can be created and used.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task Snapshot_ShouldBeCreatedAndUsed()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
            new StreamStoreObject<string>("data3", "idempotency3"),
        ];
        _ = await stream.AddAsync(items, CancellationToken.None);

        // Create a snapshot at version 2
        var snapshot = new StreamStoreObject<string>("snapshot-data", "snapshot-id");
        await stream.SnapshotAsync(2, snapshot, CancellationToken.None);

        // Act - Get all data using snapshot
        StreamStoreResult<string> result = await stream.GetAsync(true, CancellationToken.None);

        // Assert
        result.Version.ShouldBe(3);
        IStreamStoreObject<string>[] resultArray = [.. result.Objects];
        resultArray.Length.ShouldBe(2); // Snapshot + items after snapshot
        resultArray[0].Data.ShouldBe("snapshot-data"); // Should contain the snapshot
        resultArray[1].Data.ShouldBe("data3"); // Should contain items after snapshot
    }

    /// <summary>
    /// Tests that snapshot with invalid version throws exception.
    /// </summary>
    /// <returns>The task.</returns>
    [Fact]
    public async Task SnapshotWithInvalidVersion_ShouldThrow()
    {
        // Arrange
        var store = new InMemoryStreamStore<string>();
        IStoreStream<string> stream = store.GetStream("test-stream");
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
        ];
        _ = await stream.AddAsync(items, CancellationToken.None);

        var snapshot = new StreamStoreObject<string>("snapshot-data", "snapshot-id");

        // Act & Assert - Try to create a snapshot with invalid version
        _ = await Should.ThrowAsync<ArgumentException>(async () =>
            await stream.SnapshotAsync(3, snapshot, CancellationToken.None));
    }
}
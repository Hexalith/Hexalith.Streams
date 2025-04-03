// <copyright file="FileStreamStoreProvider{TData}.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Commons.Configurations;
using Hexalith.Commons.UniqueIds;
using Hexalith.Streams.Configurations;
using Hexalith.Streams.Providers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// File-based implementation of the stream store provider that stores data in directories and files.
/// </summary>
/// <typeparam name="TData">The type of the data stored in the stream.</typeparam>
public sealed partial class FileStreamStoreProvider<TData> : IStreamStoreProvider<TData>
{
    private const string LockFileName = "lock.json";
    private static readonly Regex FileNamePattern = FileNameRegex();
    private readonly string _basePath;
    private readonly TimeSpan _lockTimeout;
    private readonly ILogger<FileStreamStoreProvider<TData>> _logger;
    private readonly IStreamStoreSerializer<TData> _serializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStreamStoreProvider{TData}"/> class.
    /// </summary>
    /// <param name="options">The options containing the settings for the stream store.</param>
    /// <param name="serializer">The serializer used to serialize and deserialize stream data.</param>
    /// <param name="timeProvider">The time provider used for generating timestamps.</param>
    /// <param name="logger">The logger used for logging information and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or serializer is null.</exception>
    public FileStreamStoreProvider(
        IOptions<StreamsSettings> options,
        IStreamStoreSerializer<TData> serializer,
        TimeProvider timeProvider,
        ILogger<FileStreamStoreProvider<TData>> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        SettingsException<StreamsSettings>.ThrowIfUndefined(options.Value.FileStreamRootPath);
        SettingsException<StreamsSettings>.ThrowIfUndefined(options.Value.LockTimeout);

        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _timeProvider = timeProvider;
        _logger = logger;
        _basePath = options.Value.FileStreamRootPath;
        _lockTimeout = options.Value.LockTimeout!.Value;
    }

    /// <inheritdoc/>
    public Task CloseSessionAsync(string sessionId, string streamId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(streamId);
        ArgumentNullException.ThrowIfNull(sessionId);
        string lockFilePath = GetLockFilePath(streamId);
        if (File.Exists(lockFilePath))
        {
            File.Delete(lockFilePath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IStreamStoreObject<TData>> GetAsync(string sessionId, string streamId, long version, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string filePath = GetFileNameByVersion(streamId, version);

        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        return await _serializer
            .DeserializeAsync(fileStream, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IStreamStoreObject<TData>> GetAsync(string sessionId, string streamId, string idempotencyId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(streamId);
        ArgumentNullException.ThrowIfNull(idempotencyId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string filePath = GetFileNameByIdempotencyId(streamId, idempotencyId);

        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        return await _serializer.DeserializeAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IStreamStoreObject<TData>> GetSnapshotAsync(string sessionId, string streamId, long version, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string snapshotFilePath = GetSnapshotFilePath(streamId, version);
        await using FileStream fileStream = new(snapshotFilePath, FileMode.Open, FileAccess.Read);
        return await _serializer.DeserializeAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<long>> GetSnapshotVersionsAsync(string sessionId, string streamId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string snapshotDir = GetSnapshotDirectory(streamId);
        return [.. Directory
                .GetFiles(snapshotDir)
                .Select(file =>
                {
                    string fileName = Path.GetFileName(file);
                    if (long.TryParse(fileName.Split('.')[0], out long version))
                    {
                        return version;
                    }

                    return -1L;
                })
                .Where(v => v >= 0)
                .Order()];
    }

    /// <inheritdoc/>
    public async Task<long> GetVersionAsync(string sessionId, string streamId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string[] files = Directory.GetFiles(GetStreamDataDirectory(streamId));
        long maxVersion = 0;

        foreach (string file in files)
        {
            if (TryParseFileName(file, out long version, out _))
            {
                maxVersion = Math.Max(maxVersion, version);
            }
        }

        return maxVersion;
    }

    /// <inheritdoc/>
    public async Task<string> OpenSessionAsync(string streamId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(streamId);
        string lockFilePath = GetLockFilePath(streamId);
        TimeSpan timeout = _lockTimeout.Add(TimeSpan.FromSeconds(1));
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        do
        {
            try
            {
                await using FileStream fileStream = new(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                if (fileStream.Length > 0)
                {
                    FileStreamStoreLock? previousLock = await JsonSerializer.DeserializeAsync<FileStreamStoreLock>(fileStream, options: null, cancellationToken)
                        ?? throw new InvalidOperationException($"Failed to deserialize lock file for stream: {streamId}");
                    if (previousLock.Expiration < _timeProvider.GetUtcNow())
                    {
                        // Lock expired, clear the file and create a new lock
                        fileStream.SetLength(0);
                    }
                    else
                    {
                        // Lock is still valid, wait and retry
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }

                // Create a new lock
                FileStreamStoreLock streamLock = new(UniqueIdHelper.GenerateUniqueStringId(), _timeProvider.GetUtcNow().Add(_lockTimeout));
                await using FileStream lockFileStream = new(lockFilePath, FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(lockFileStream, streamLock, options: null, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return streamLock.SessionId;
            }
            catch (IOException)
            {
                // File is locked by another process, wait and retry
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }
        while (stopWatch.Elapsed <= timeout);
        throw new InvalidOperationException($"Session already exists for stream: {streamId}. Close the session before opening a new one.");
    }

    /// <inheritdoc/>
    public async Task RemoveSnapshotAsync(string sessionId, string streamId, long version, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string snapshotFilePath = GetSnapshotFilePath(streamId, version);

        if (File.Exists(snapshotFilePath))
        {
            File.Delete(snapshotFilePath);
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(string sessionId, string streamId, IStreamStoreObject<TData> value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string filePath = GetStreamFilePath(streamId, value.Version, value.IdempotencyId);

        await using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
        await _serializer.SerializeAsync(fileStream, value, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetSnapshotAsync(string sessionId, string streamId, IStreamStoreObject<TData> value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await ValidateSessionAsync(sessionId, streamId, cancellationToken);

        string snapshotFilePath = GetSnapshotFilePath(streamId, value.Version);

        await using FileStream fileStream = new(snapshotFilePath, FileMode.Create, FileAccess.Write);
        await _serializer.SerializeAsync(fileStream, value, cancellationToken).ConfigureAwait(false);
    }

    [GeneratedRegex(@"^(\d+)\.(.+)\.(.+)$", RegexOptions.Compiled)]
    private static partial Regex FileNameRegex();

    /// <summary>
    /// Parses a file name to extract version and idempotency ID.
    /// </summary>
    /// <param name="fileName">The file name to parse.</param>
    /// <param name="version">The parsed version number.</param>
    /// <param name="idempotencyId">The parsed idempotency ID.</param>
    /// <returns>True if parsing was successful, false otherwise.</returns>
    private static bool TryParseFileName(string fileName, out long version, out string idempotencyId)
    {
        Match match = FileNamePattern.Match(Path.GetFileName(fileName));
        if (match.Success && long.TryParse(match.Groups[1].Value, out version))
        {
            idempotencyId = match.Groups[2].Value;
            return true;
        }

        version = 0;
        idempotencyId = string.Empty;
        return false;
    }

    private string GetFileNameByIdempotencyId(string streamId, string idempotencyId)
    {
        string streamDir = GetStreamDirectory(streamId);
        string[] files = Directory.GetFiles(streamDir, $"*.{idempotencyId}.*");
        if (files.Length == 0)
        {
            throw new FileNotFoundException($"Idempotency Id {idempotencyId} not found in stream: {streamId}");
        }

        if (files.Length > 1)
        {
            throw new InvalidOperationException(
                $"Duplicate idempotency identifier {idempotencyId} found in stream {streamId}. Files : {string.Join("; ", files)}");
        }

        return files[0];
    }

    private string GetFileNameByVersion(string streamId, long version)
    {
        string streamDir = GetStreamDataDirectory(streamId);
        string[] files = Directory.GetFiles(streamDir, $"{version}.*.*");
        if (files.Length == 0)
        {
            throw new FileNotFoundException($"Version {version} not found in stream: {streamId}");
        }

        if (files.Length > 1)
        {
            throw new InvalidOperationException(
                $"Duplicate version {version} found in stream {streamId}. Files : {string.Join("; ", files)}");
        }

        return files[0];
    }

    private string GetLockFilePath(string streamId)
                                                                => Path.Combine(GetStreamDirectory(streamId), LockFileName);

    /// <summary>
    /// Gets the snapshot directory path for a given stream ID.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>The full directory path for the stream's snapshots.</returns>
    private string GetSnapshotDirectory(string streamId)
    {
        string path = Path.Combine(GetStreamDataDirectory(streamId), "Snapshots");
        if (!Directory.Exists(path))
        {
            _ = Directory.CreateDirectory(path);
        }

        return path;
    }

    /// <summary>
    /// Gets the file path for a specific snapshot version of a stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version number.</param>
    /// <returns>The full file path.</returns>
    private string GetSnapshotFilePath(string streamId, long version)
    {
        string filePath = Path.Combine(GetSnapshotDirectory(streamId), $"{version}.*");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Snapshot version {version} not found for stream: {streamId}");
        }

        return filePath;
    }

    private string GetStreamDataDirectory(string streamId)
    {
        string path = Path.Combine(GetStreamDirectory(streamId), "Data");
        if (!Directory.Exists(path))
        {
            _ = Directory.CreateDirectory(path);
        }

        return path;
    }

    /// <summary>
    /// Gets the stream directory path for a given stream ID.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>The full directory path for the stream.</returns>
    private string GetStreamDirectory(string streamId)
    {
        string path = Path.Combine(_basePath, streamId);
        if (!Directory.Exists(path))
        {
            _ = Directory.CreateDirectory(path);
        }

        return path;
    }

    /// <summary>
    /// Gets the file path for a specific version of a stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version number.</param>
    /// <param name="idempotencyId">The idempotency identifier.</param>
    /// <returns>The full file path.</returns>
    private string GetStreamFilePath(string streamId, long version, string idempotencyId)
        => Path.Combine(GetStreamDataDirectory(streamId), $"{version}.{idempotencyId}.{_serializer.SerializationType}");

    private async Task ValidateSessionAsync(string sessionId, string streamId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(streamId);
        ArgumentNullException.ThrowIfNull(sessionId);
        string lockFilePath = GetLockFilePath(streamId);
        if (!File.Exists(lockFilePath))
        {
            throw new FileNotFoundException($"Session {sessionId} not found for stream: {streamId}. Open a session before accessing the stream.");
        }

        await using FileStream fileStream = new(lockFilePath, FileMode.Open, FileAccess.Read);
        FileStreamStoreLock? streamLock = await JsonSerializer.DeserializeAsync<FileStreamStoreLock>(fileStream, options: null, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize lock file for stream: {streamId}");
        if (streamLock.SessionId != sessionId)
        {
            throw new InvalidOperationException($"Session {sessionId} does not match the lock file for stream: {streamId}");
        }
    }
}
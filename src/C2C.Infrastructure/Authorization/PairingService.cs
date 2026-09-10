using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;
using C2C.Core.Common;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// Domain service implementing UC-AUTH-01 pairing session lifecycle adhering to BR-AUTH-007, BR-AUTH-008, BR-SEC-011, and BR-COM-009.
/// Plaintext pairing codes are generated using CSPRNG and returned to the caller once; only SHA-256 hashes are persisted.
/// </summary>
public sealed class PairingService : IPairingService
{
    private const string AllowedCharacters = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private readonly IPairingStore _pairingStore;
    private readonly PairingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PairingService(
        IPairingStore pairingStore,
        PairingOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _pairingStore = pairingStore ?? throw new ArgumentNullException(nameof(pairingStore));
        _options = options ?? new PairingOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<PairingCreateResult> CreateSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string displayCode = RandomNumberGenerator.GetString(AllowedCharacters, _options.CodeLength);
            string codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(displayCode)));

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTimeOffset expiresAt = now.Add(_options.Ttl);
            string sessionId = Guid.NewGuid().ToString("N");

            PairingSession session = new()
            {
                PairingSessionId = sessionId,
                WorkspaceId = workspaceId,
                CodeHash = codeHash,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                AttemptsRemaining = _options.MaxAttempts,
                Status = PairingSessionStatus.Active,
                SchemaVersion = 1
            };

            await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);

            return new PairingCreateResult
            {
                PairingSessionId = sessionId,
                DisplayCode = displayCode,
                ExpiresAt = expiresAt
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PairingSession?> GetCurrentSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            PairingSession? session = await _pairingStore.GetSessionAsync(workspaceId, cancellationToken);
            if (session == null)
            {
                return null;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (session.Status == PairingSessionStatus.Active && now >= session.ExpiresAt)
            {
                session.Status = PairingSessionStatus.Expired;
                await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);
            }

            return session;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PairingValidationResult> ValidateAndConsumeCodeAsync(
        string workspaceId,
        string pairingCode,
        string? clientId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pairingCode);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            PairingSession? session = await _pairingStore.GetSessionAsync(workspaceId, cancellationToken);
            if (session == null)
            {
                return PairingValidationResult.Failed(
                    CommonErrorCodes.AuthPairingInvalid,
                    "No pairing session found for this workspace.");
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();

            if (session.Status == PairingSessionStatus.Expired || now >= session.ExpiresAt)
            {
                if (session.Status != PairingSessionStatus.Expired)
                {
                    session.Status = PairingSessionStatus.Expired;
                    await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);
                }

                return PairingValidationResult.Failed(
                    CommonErrorCodes.AuthPairingExpired,
                    "The pairing session has expired.");
            }

            if (session.Status == PairingSessionStatus.Locked || session.AttemptsRemaining <= 0)
            {
                if (session.Status != PairingSessionStatus.Locked)
                {
                    session.Status = PairingSessionStatus.Locked;
                    await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);
                }

                return PairingValidationResult.Failed(
                    CommonErrorCodes.AuthPairingLocked,
                    "The pairing session is locked due to too many failed attempts.");
            }

            if (session.Status == PairingSessionStatus.Consumed)
            {
                return PairingValidationResult.Failed(
                    CommonErrorCodes.AuthPairingInvalid,
                    "The pairing session has already been consumed.");
            }

            string inputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pairingCode)));
            byte[] inputHashBytes = Encoding.UTF8.GetBytes(inputHash);
            byte[] storedHashBytes = Encoding.UTF8.GetBytes(session.CodeHash);

            bool isMatch = CryptographicOperations.FixedTimeEquals(inputHashBytes, storedHashBytes);

            if (isMatch)
            {
                session.Status = PairingSessionStatus.Consumed;
                session.ApprovedClientId = clientId;
                await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);

                return PairingValidationResult.Success(session);
            }

            session.AttemptsRemaining--;
            if (session.AttemptsRemaining <= 0)
            {
                session.Status = PairingSessionStatus.Locked;
                await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);

                return PairingValidationResult.Failed(
                    CommonErrorCodes.AuthPairingLocked,
                    "Incorrect pairing code. Maximum attempts exceeded; session locked.");
            }

            await _pairingStore.SaveSessionAsync(workspaceId, session, cancellationToken);

            return PairingValidationResult.Failed(
                CommonErrorCodes.AuthPairingInvalid,
                $"Incorrect pairing code. {session.AttemptsRemaining} attempt(s) remaining.");
        }
        finally
        {
            _lock.Release();
        }
    }
}

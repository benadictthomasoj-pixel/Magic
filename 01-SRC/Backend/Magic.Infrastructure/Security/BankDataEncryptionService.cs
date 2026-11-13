using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Magic.Infrastructure.Security;

public interface IBankDataEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string Mask(string plaintext);
}

public sealed class AesGcmBankDataEncryptionService : IBankDataEncryptionService
{
    private readonly byte[] _key;

    public AesGcmBankDataEncryptionService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var keyHex = configuration["Security:BankEncryptionKey"];
        if (string.IsNullOrWhiteSpace(keyHex))
        {
            throw new InvalidOperationException(
                "CRITICAL SECURITY CONFIGURATION ERROR: 'Security:BankEncryptionKey' is required. " +
                "A 256-bit AES-GCM encryption key (64 hex characters) must be configured via environment variable " +
                "'SECURITY__BANKENCRYPTIONKEY' or configuration.");
        }

        var trimmed = keyHex.Trim();
        if (trimmed.Length != 64)
        {
            throw new InvalidOperationException(
                $"CRITICAL SECURITY CONFIGURATION ERROR: 'Security:BankEncryptionKey' must be exactly 64 hex characters (256 bits). Provided length was {trimmed.Length}.");
        }

        try
        {
            _key = Convert.FromHexString(trimmed);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "CRITICAL SECURITY CONFIGURATION ERROR: 'Security:BankEncryptionKey' is not a valid hex string.", ex);
        }
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
        var ciphertext = new byte[plaintextBytes.Length];

        using var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combined payload: nonce (12) + tag (16) + ciphertext
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return string.Empty;
        }

        var combined = Convert.FromBase64String(ciphertext);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        if (combined.Length < nonceSize + tagSize)
        {
            throw new CryptographicException("Invalid ciphertext payload size.");
        }

        var nonce = new byte[nonceSize];
        var tag = new byte[tagSize];
        var ciphertextLength = combined.Length - nonceSize - tagSize;
        var encryptedBytes = new byte[ciphertextLength];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
        Buffer.BlockCopy(combined, nonceSize + tagSize, encryptedBytes, 0, ciphertextLength);

        var plaintextBytes = new byte[ciphertextLength];
        using var aesGcm = new AesGcm(_key, tagSize);
        aesGcm.Decrypt(nonce, encryptedBytes, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public string Mask(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return string.Empty;
        }

        var trimmed = plaintext.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('•', trimmed.Length);
        }

        var last4 = trimmed[^4..];
        return $"•••• •••• {last4}";
    }
}

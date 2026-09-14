using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using DocQa.Core.Answering;
using DocQa.Core.Configuration;

namespace DocQa.Core.Corpus;

/// <summary>
/// Fingerprints the corpus's document blocks, so that a change in what is sent is reported instead of
/// surfacing as a surprise cache write.
/// </summary>
/// <remarks>
/// <para>
/// The hash covers every field of every block, in order: text, title, context, the citation setting and
/// the corpus cache breakpoint with its TTL. Any change to one of them means a cached copy of the previous
/// corpus no longer matches.
/// </para>
/// <para>
/// The serialization is written field by field in a fixed order, rather than by serializing the SDK's
/// types, whose property order is not guaranteed. A test checks it against what the SDK actually sends,
/// so a field added to <see cref="MessageRequestBuilder.DocumentBlocks"/> cannot be left out. The
/// fingerprint does not equal the prototype's, and does not need to.
/// </para>
/// </remarks>
public static class CorpusFingerprint
{
    /// <summary>Fingerprints the document blocks built from <paramref name="documents"/>.</summary>
    /// <param name="documents">The documents, in send order.</param>
    /// <returns>The SHA-256 of the blocks' canonical serialization, in lowercase hex.</returns>
    public static string Compute(IReadOnlyList<RequestDocument> documents) =>
        Convert.ToHexStringLower(SHA256.HashData(CanonicalJson(documents)));

    /// <summary>The document blocks as the Messages API receives them, as UTF-8 JSON in a fixed field order.</summary>
    internal static byte[] CanonicalJson(IReadOnlyList<RequestDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            for (int i = 0; i < documents.Count; i++)
            {
                RequestDocument document = documents[i];
                writer.WriteStartObject();
                writer.WriteString("type", "document");

                writer.WriteStartObject("source");
                writer.WriteString("type", "text");
                writer.WriteString("media_type", "text/plain");
                writer.WriteString("data", document.Text);
                writer.WriteEndObject();

                writer.WriteString("title", document.Title);
                writer.WriteString("context", document.Context);

                writer.WriteStartObject("citations");
                writer.WriteBoolean("enabled", true);
                writer.WriteEndObject();

                // As in MessageRequestBuilder: one breakpoint, on the last block, closes the corpus prefix.
                if (i == documents.Count - 1)
                {
                    writer.WriteStartObject("cache_control");
                    writer.WriteString("type", "ephemeral");
                    writer.WriteString("ttl", CacheAffecting.CorpusCacheTtl);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        return buffer.WrittenSpan.ToArray();
    }
}

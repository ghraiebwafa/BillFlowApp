namespace BillFlow.Shared.Email;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }

    public required byte[] Content { get; init; }

    public string ContentType { get; init; } = "application/octet-stream";
}

public sealed class EmailMessage
{
    public required string ToEmail { get; init; }

    public string? ToName { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }

    public string? PlainTextBody { get; init; }

    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];
}

public sealed record EmailSendResult(bool Success, bool Skipped, string? Detail);

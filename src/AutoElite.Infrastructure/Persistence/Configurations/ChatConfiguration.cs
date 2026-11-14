using AutoElite.Infrastructure.Encryption;
using AutoElite.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoElite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Chat"/> with its memberships. A pair of users has at most one personal chat and a study group at
/// most one chat, which unique indexes make sure of when two requests create one at once.
/// </summary>
internal sealed class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    /// <summary>The foreign key of the memberships to the chat.</summary>
    public const string ChatKey = "ChatId";

    /// <summary>The length of a personal chat key: two identifiers in the "N" format and a colon.</summary>
    private const int DirectKeyLength = 65;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ConfigureAggregateRoot("chats");

        builder.Ignore(chat => chat.CurrentMembers);
        builder.Property(chat => chat.Title).HasMaxLength(Chat.MaxTitleLength);
        builder.Property(chat => chat.DirectKey).HasMaxLength(DirectKeyLength);
        builder.HasIndex(chat => chat.DirectKey).IsUnique().HasFilter("direct_key IS NOT NULL");
        builder.HasOne<StudyGroup>().WithMany().HasForeignKey(chat => chat.StudyGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(chat => chat.StudyGroupId).IsUnique().HasFilter("study_group_id IS NOT NULL");

        builder.HasMany(chat => chat.Members).WithOne().HasForeignKey(ChatKey).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(chat => chat.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// Maps <see cref="ChatMember"/>; a user has at most one current membership of a chat, while ended ones stay as
/// history.
/// </summary>
internal sealed class ChatMemberConfiguration : IEntityTypeConfiguration<ChatMember>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatMember> builder)
    {
        builder.ConfigureChild("chat_members", ChatConfiguration.ChatKey);

        builder.HasIndex(ChatConfiguration.ChatKey, nameof(ChatMember.UserId))
            .IsUnique()
            .HasFilter("left_at_utc IS NULL")
            .HasDatabaseName("ux_chat_members_chat_user_current");
        builder.HasIndex(member => member.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Maps <see cref="ChatMessage"/>, an aggregate of its own. The text is stored encrypted, so it has no length limit
/// of its own: the domain limits the plain text. The attachments are an array of document identifiers with a GIN
/// index, so that finding the messages a document is attached to does not read the whole table. It needs the cipher, so
/// it is not an <see cref="IEntityTypeConfiguration{TEntity}"/>, which the scan of the assembly would try to create
/// without one; the context applies it itself.
/// </summary>
/// <param name="cipher">The cipher of the text.</param>
internal sealed class ChatMessageConfiguration(ChatTextCipher cipher)
{
    /// <summary>Configures the entity.</summary>
    /// <param name="builder">The builder of the entity.</param>
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ConfigureAggregateRoot("chat_messages");

        builder.Property(message => message.Text).HasConversion(new EncryptedTextConverter(cipher));
        builder.PrimitiveCollection(message => message.AttachmentIds)
            .HasField("_attachmentIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(message => message.AttachmentIds).HasMethod("gin");
        builder.HasOne<Chat>().WithMany().HasForeignKey(message => message.ChatId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(message => message.SenderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(message => new { message.ChatId, message.SentAtUtc, message.Id });
    }
}

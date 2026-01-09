using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Notification.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> b)
        {
            b.ToTable("Notifications");

            b.HasKey(x => x.Id);

            b.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(64);

            b.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.TitleUnsign)
                .HasMaxLength(200);

            b.Property(x => x.Message)
                .IsRequired();

            b.Property(x => x.Href)
                .HasMaxLength(512);

            // Enum -> int
            b.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();


            b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        }
    }
}

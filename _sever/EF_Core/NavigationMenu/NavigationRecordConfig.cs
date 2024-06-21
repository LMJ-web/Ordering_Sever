using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _sever.EF_Core.NavigationMenu
{
    public class NavigationRecordConfig : IEntityTypeConfiguration<NavigationRecord>
    {
        public void Configure(EntityTypeBuilder<NavigationRecord> builder)
        {
            builder.ToTable("T_NavigationMenu");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.NavigationName).IsRequired();
        }
    }
}

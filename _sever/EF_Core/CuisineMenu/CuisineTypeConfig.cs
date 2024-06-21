using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _sever.EF_Core.CuisineMenu
{
    public class CuisineTypeConfig : IEntityTypeConfiguration<CuisineType>
    {
        public void Configure(EntityTypeBuilder<CuisineType> builder)
        {
            builder.ToTable("T_CuisineType");
            builder.HasKey(x => x.Id);
        }
    }
}

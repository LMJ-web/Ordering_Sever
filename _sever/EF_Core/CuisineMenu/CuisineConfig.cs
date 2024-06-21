using _sever.EF_Core.NavigationMenu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _sever.EF_Core.CuisineMenu
{
    public class CuisineConfig : IEntityTypeConfiguration<Cuisine>
    {
        public void Configure(EntityTypeBuilder<Cuisine> builder)
        {
            builder.ToTable("T_Cuisine");//指定表名
            builder.HasKey(cuisine => cuisine.Id);//指定主键
            builder.Property(cuisine => cuisine.CuisineName).IsRequired();//非空约束
            builder.Property(cuisine => cuisine.CuisinePrice).HasColumnType("decimal(7, 2)");
            builder.HasOne<CuisineType>(cuisine => cuisine.Cuisine_Type).WithMany().HasForeignKey(cuisine=>cuisine.T_CuisineType_Id);
            builder.Ignore(cuisine => cuisine.Number);
            builder.Ignore(cuisine => cuisine.CustomerInfos);
        }
    }
}

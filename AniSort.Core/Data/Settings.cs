using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class Setting
{
    [Key]
    public int Id { get; set; }

    public Config Config { get; set; }
}

public class SettingEntityTypeConfiguration : IEntityTypeConfiguration<Setting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.Property(e => e.Config)
            .HasConversion(
                c => Constants.YamlSerializer.Serialize(c),
                c => Constants.YamlDeserializer.Deserialize<Config>(c));
    }
}

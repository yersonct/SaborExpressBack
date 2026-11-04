using System.ComponentModel.DataAnnotations;
namespace SaborExpress.Shared.Responses;
public class PageQuery
{
    [Range(1, 100000)] public int? Page { get; set; }
    [Range(1, 100)] public int? PageSize { get; set; }
    public bool IsRequested => Page.HasValue || PageSize.HasValue;
    public int Offset => (Math.Clamp(Page ?? 1, 1, 100000) - 1) * Limit;
    public int Limit => Math.Clamp(PageSize ?? 100, 1, 100);
}

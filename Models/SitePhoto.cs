public class SitePhoto
{
    public int Id {get; set; }
    public int SiteId{get; set;}
    public Site? Site {get; set;}
    public string FileName{get; set;} = string.Empty;
}
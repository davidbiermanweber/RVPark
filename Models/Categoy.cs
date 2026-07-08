public class Category
{
    public int Id { get; set;}
    public string Name {get; set;} = string.Empty;

    // A site type has many dated prices
    public List<CategoryPrice> Prices { get; set; } = new();
}

namespace Dfe.Spi.Registry.Domain.Links
{
    public class Link
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public EntityLink[] LinkedEntities { get; set; }
    }
}
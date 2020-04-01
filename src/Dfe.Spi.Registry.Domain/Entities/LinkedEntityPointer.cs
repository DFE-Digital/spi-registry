namespace Dfe.Spi.Registry.Domain.Entities
{
    public class LinkedEntityPointer : EntityPointer
    {
        public string LinkType { get; set; }
        public string EntityType { get; set; }
    }
}
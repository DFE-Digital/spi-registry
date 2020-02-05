using System;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace PopulateRegistryFromGiasEstablishments
{
    class CsvFileParser<T> : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly CsvReader _csv;

        public CsvFileParser(StreamReader reader, ClassMap<T> mapping)
        {
            _reader = reader;
            _csv = new CsvReader(_reader);
            _csv.Configuration.RegisterClassMap(mapping);

            // TODO: Handle missing fields
            _csv.Configuration.HeaderValidated = null;
            _csv.Configuration.MissingFieldFound = null;
        }

        public T[] GetRecords()
        {
            return _csv.GetRecords<T>().ToArray();
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _csv?.Dispose();
        }
    }

    class EstablishmentCsvMapping: ClassMap<Establishment>
    {
        public EstablishmentCsvMapping()
        {
            Map(x => x.Urn).Name("URN");
            Map(x => x.Ukprn).Name("UKPRN");
            Map(x => x.Name).Name("EstablishmentName");
        }
    }

    class Establishment
    {
        public long Urn { get; set; }
        public long? Ukprn { get; set; }
        public string Name { get; set; }
    }
}
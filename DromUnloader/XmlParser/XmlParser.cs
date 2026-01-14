using System.Text;
using System.Xml.Linq;

namespace DromUnloader.XmlParser;

public class XmlParser : IDisposable
{
    private readonly HttpClient httpClient;
    private bool disposeClient;
    
    private const string SheetId = "19hN3-4jTK1fXIuQ9SV9HKQSmjh9UbhP3bxRuFQud3u8";

    private const string SheetCsvUrl = $"https://docs.google.com/spreadsheets/d/{SheetId}/export?format=csv&gid=0";

    public XmlParser(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            this.httpClient = new HttpClient();
            disposeClient = true;
        }
        else
        {
            this.httpClient = httpClient;
        }
    }

    /// <summary>
    /// Получает CSV из Google Sheet
    /// </summary>
    private async Task<XDocument> BuildFeedAsync(CancellationToken cancellationToken = default)
    {
        var csv = await httpClient.GetStringAsync(SheetCsvUrl, cancellationToken);

        var rows = ParseCsv(csv).ToList();
        if (rows.Count == 0)
            throw new InvalidOperationException("Google Sheet вернул пустой CSV.");

        var header = rows[0];
        var dataRows = rows.Skip(1);

        var root = new XElement("avtoxml");
        var motoOffers = new XElement("MotoOffers");
        root.Add(motoOffers);

        foreach (var row in dataRows)
        {
            if (row.Length == 0)
                continue;

            var dict = RowToDictionary(header, row);

            if (!dict.TryGetValue("idOffer", out var idOffer) || string.IsNullOrWhiteSpace(idOffer))
                continue;

            var offer = new XElement("MotoOffer",
                new XElement("idOffer", idOffer),

                El("VIN", dict),
                El("idModelMoto", dict),
                El("YearOfMade", dict),
                El("idNewType", dict),
                El("idHaulRussiaType", dict),
                El("Haul", dict),
                El("Volume", dict),
                El("idEngineTypeMoto", dict),
                El("EngineStrokeType", dict),
                El("FuelSupplyType", dict),
                El("idFrameTypeMoto", dict),
                El("idTransmissionMoto", dict),
                El("idCountry", dict),
                El("Whereabouts", dict),
                El("idDamagedType", dict),
                El("HasNoDocs", dict),
                El("Price", dict),
                El("idCurrency", dict),
                El("idCity", dict),
                El("Phone", dict),
                El("Additional", dict)
            );

            motoOffers.Add(offer);
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            root
        );

        return doc;
    }
    
    public async Task<string> BuildAvitoFeedStringAsync(CancellationToken cancellationToken = default)
    {
        var doc = await BuildFeedAsync(cancellationToken);
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static Dictionary<string, string> RowToDictionary(string[] header, string[] row)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var len = Math.Min(header.Length, row.Length);
        for (var i = 0; i < len; i++)
        {
            var key = header[i].Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            dict[key] = row[i]?.Trim() ?? string.Empty;
        }

        return dict;
    }

    private static XElement? El(string columnName, IDictionary<string, string> dict)
    {
        return dict.TryGetValue(columnName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? new XElement(columnName, value)
            : null;
    }
    
    private static IEnumerable<string[]> ParseCsv(string csv)
    {
        using var reader = new StringReader(csv);

        while (reader.ReadLine() is { } line)
        {
            yield return SplitCsvLine(line).ToArray();
        }
    }

    private static IEnumerable<string> SplitCsvLine(string? line)
    {
        if (line == null)
        {
            yield break;
        }

        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            switch (c)
            {
                case '"' when inQuotes && i + 1 < line.Length && line[i + 1] == '"':
                    sb.Append('"');
                    i++;
                    break;
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case ',' when !inQuotes:
                    yield return sb.ToString();
                    sb.Clear();
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        yield return sb.ToString();
    }

    public void Dispose()
    {
        if (disposeClient)
            httpClient.Dispose();
    }
}

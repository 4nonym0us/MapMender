namespace MapMender.Slk;

/// <summary>
/// Basic Symbolic Link (SYLK) reader for *.SLK files.
/// </summary>
public class SlkReader
{
    public class UnitData
    {
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    private class RowData
    {
        public Dictionary<int, string> Values { get; } = new();
    }

    /// <summary>
    /// Processes a stream of SYLK file and returns a dictionary, which represents stored unit data.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static Dictionary<string, UnitData> Read(Stream stream)
    {
        var columnHeaders = new Dictionary<int, string>();
        var rowData = new Dictionary<int, RowData>();
        var currentY = 0;

        using (var reader = new StreamReader(stream))
        {
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith("C"))
                {
                    ProcessCRecord(line, columnHeaders, rowData, ref currentY);
                }
            }
        }

        // Convert row data to unit data
        return ConvertToUnitData(rowData, columnHeaders);
    }

    /// <summary>
    /// Processes SYLK file and returns a dictionary, which represents stored unit data.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static Dictionary<string, UnitData> Read(string filePath)
    {
        using var fs = File.OpenRead(filePath);

        return Read(fs);
    }

    private static void ProcessCRecord(string record,
        Dictionary<int, string> columnHeaders,
        Dictionary<int, RowData> rowData,
        ref int currentY)
    {
        int x = 0;
        string? value = null;
        var elements = record[2..].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var element in elements)
        {
            if (element.StartsWith("X"))
            {
                x = int.Parse(element[1..]);
            }
            else if (element.StartsWith("Y"))
            {
                currentY = int.Parse(element[1..]);
            }
            else if (element.StartsWith("K"))
            {
                value = element[1..].Trim('"');
            }
            else
            {
                throw new ArgumentException("Failed to parse");
            }
        }

        // If this is the header row (Y=1), store column headers
        if (currentY == 1)
        {
            if (value != null)
            {
                columnHeaders[x] = value;
            }
        }
        // Otherwise store the value in the appropriate row
        else if (currentY > 1)
        {
            if (!rowData.ContainsKey(currentY))
            {
                rowData[currentY] = new RowData();
            }

            if (value != null)
            {
                rowData[currentY].Values[x] = value;
            }
        }
    }

    private static Dictionary<string, UnitData> ConvertToUnitData(
        Dictionary<int, RowData> rowData,
        Dictionary<int, string> columnHeaders)
    {
        var units = new Dictionary<string, UnitData>();

        foreach (var row in rowData.Values)
        {
            // Get the unit ID (should be in column 1 or 0)
            var unitIdRow = 1;
            if (!row.Values.ContainsKey(unitIdRow))
            {
                unitIdRow = 0;
            }

            if (!row.Values.TryGetValue(unitIdRow, out var unitId))
            {
                continue;
            }

            if (string.IsNullOrEmpty(unitId))
            {
                continue;
            }

            // Create or get the unit data
            if (!units.ContainsKey(unitId))
            {
                units[unitId] = new UnitData();
            }

            // Add all properties for this row
            foreach (var cell in row.Values)
            {
                if (columnHeaders.TryGetValue(cell.Key, out var propertyName))
                {
                    units[unitId].Properties[propertyName] = cell.Value;
                }
            }
        }

        return units;
    }
}
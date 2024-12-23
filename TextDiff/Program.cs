using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    static void Main(string[] args)
    {
        string oldFilePath = "/app/old.json"; // Path to older JSON file
        string newFilePath = "/app/new.json"; // Path to new JSON file
        string outputFilePath = "/app/differences.txt"; // Output file for differences

        // Load and parse the JSON files
        var oldFileLines = File.ReadAllLines(oldFilePath);
        var newFileLines = File.ReadAllLines(newFilePath);
        JObject oldJson = LoadJsonFile(oldFilePath);
        JObject newJson = LoadJsonFile(newFilePath);

        // Get the components section
        JArray oldComponents = (JArray)oldJson["components"] ?? new JArray();
        JArray newComponents = (JArray)newJson["components"] ?? new JArray();

        // Find differences between the two components arrays
        var differences = CompareComponents(oldComponents, newComponents, oldFileLines, newFileLines);

        // Write differences to output file
        File.WriteAllLines(outputFilePath, differences);

        // Print differences to console
        if (differences.Count == 0)
        {
            Console.WriteLine("No differences found.");
        }
        else
        {
            Console.WriteLine("=== Differences ===");

            foreach (var diff in differences)
            {
                Console.WriteLine(diff);
            }
            Console.WriteLine($"\nDifferences written to {outputFilePath}");
        }
    }

    static JObject LoadJsonFile(string filePath)
    {
        string jsonContent = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<JObject>(jsonContent);
    }

    static List<string> CompareComponents(JArray oldComponents, JArray newComponents, string[] oldFileLines, string[] newFileLines)
    {
        var differences = new List<string>();
        string GetBaseName(string bomRef) => bomRef?.Split('@')[0];
        var oldPackages = oldComponents.GroupBy(obj => GetBaseName((string)obj["bom-ref"]))
                                       .ToDictionary(g => g.Key, g => g.First());
        var newPackages = newComponents.GroupBy(obj => GetBaseName((string)obj["bom-ref"]))
                                       .ToDictionary(g => g.Key, g => g.First());

        // Removed packages
        foreach (var oldKey in oldPackages.Keys.Except(newPackages.Keys))
        {
            int lineNumber = FindLineNumber(oldFileLines, (string)oldPackages[oldKey]["bom-ref"]);
            differences.Add($"\nRemoved: Package '{oldKey}' (Line: {lineNumber})");
            differences.Add($"Details:\n{oldPackages[oldKey].ToString(Formatting.Indented)}");
        }

        // Added packages
        foreach (var newKey in newPackages.Keys.Except(oldPackages.Keys))
        {
            int lineNumber = FindLineNumber(newFileLines, (string)newPackages[newKey]["bom-ref"]);
            differences.Add($"\nAdded: Package '{newKey}' (Line: {lineNumber})");
            differences.Add($"Details:\n{newPackages[newKey].ToString(Formatting.Indented)}");
        }

        // Compare existing packages
        foreach (var commonKey in oldPackages.Keys.Intersect(newPackages.Keys))
        {
            var oldPackage = oldPackages[commonKey];
            var newPackage = newPackages[commonKey];
            string oldBomRef = (string)oldPackage["bom-ref"];
            string newBomRef = (string)newPackage["bom-ref"];

            if (oldBomRef != newBomRef)
            {
                int oldLineNumber = FindLineNumber(oldFileLines, oldBomRef);
                int newLineNumber = FindLineNumber(newFileLines, newBomRef);
                differences.Add($"\nChanged: Package '{commonKey}'");
                differences.Add($"   Old bom-ref (Line {oldLineNumber}): {oldBomRef}");
                differences.Add($"   New bom-ref (Line {newLineNumber}): {newBomRef}");
            }
            CompareSpecificFields(oldPackage, newPackage, differences, commonKey, oldFileLines, newFileLines);
        }
        return differences;
    }

    static void CompareSpecificFields(JToken oldPackage, JToken newPackage, List<string> differences, string packageName, string[] oldFileLines, string[] newFileLines)
    {
        CompareField(oldPackage, newPackage, differences, packageName, "name", oldFileLines, newFileLines);
        CompareField(oldPackage, newPackage, differences, packageName, "version", oldFileLines, newFileLines);
        CompareField(oldPackage, newPackage, differences, packageName, "licenses", oldFileLines, newFileLines);
        CompareField(oldPackage, newPackage, differences, packageName, "authors", oldFileLines, newFileLines);
        CompareField(oldPackage, newPackage, differences, packageName, "copyright", oldFileLines, newFileLines); // Add copyright check
    }

    static void CompareField(JToken oldToken, JToken newToken, List<string> differences, string packageName, string fieldName, string[] oldFileLines, string[] newFileLines)
    {
        var oldField = oldToken[fieldName];
        var newField = newToken[fieldName];

        if (!JToken.DeepEquals(oldField, newField))
        {
            int oldLineNumber = FindFieldLineNumber(oldFileLines, (string)oldToken["bom-ref"], fieldName);
            int newLineNumber = FindFieldLineNumber(newFileLines, (string)newToken["bom-ref"], fieldName);

            differences.Add($"\nChanged: {packageName}.{fieldName}");
            differences.Add($"   Old Value (Line {oldLineNumber}): {oldField ?? "Not Present"}");
            differences.Add($"   New Value (Line {newLineNumber}): {newField ?? "Not Present"}");
        }
    }



    static int FindLineNumber(string[] fileLines, string identifier)
    {
        Console.WriteLine($"Searching for identifier: {identifier}");
        
        for (int i = 0; i < fileLines.Length; i++)
        {
            if (fileLines[i].Contains(identifier))
            {
                Console.WriteLine($"Found '{identifier}' at line {i + 1}: {fileLines[i]}");
                return i + 1;
            }
        }
        
        Console.WriteLine($"Identifier '{identifier}' not found.");
        return -1;
    }

    static int FindFieldLineNumber(string[] fileLines, string identifier, string fieldName)
    {
        for (int i = 0; i < fileLines.Length; i++)
        {
            string line = fileLines[i].Trim();

            // Locate the bom-ref line
            if (line.Contains(identifier) && line.Contains("bom-ref"))
            {
                return i + 1; // Return the line number of the bom-ref
            }
        }

        // If the bom-ref is not found, return -1
        return -1;
    }


}

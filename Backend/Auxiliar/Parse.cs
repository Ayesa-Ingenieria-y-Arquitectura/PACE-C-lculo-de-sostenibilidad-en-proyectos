using Bc3_WPF.backend.Modelos;
using System.IO;
using System.Text.Json;


namespace Bc3_WPF.backend.Auxiliar
{
    internal class Parse
    {
        public static DateOnly? ParseDate(string date)
        {
            if (date.Length == 6)
            {
                int day = int.Parse(date.Substring(0, 2));
                int month = int.Parse(date.Substring(2, 2));
                int year = int.Parse(date.Substring(4));
                year += 2000;

                return new DateOnly(year, month, day);
            }
            if (date.Length == 8)
            {
                int day = int.Parse(date.Substring(0, 2));
                int month = int.Parse(date.Substring(2, 2));
                int year = int.Parse(date.Substring(4));

                return new DateOnly(year, month, day);
            }
            return null;
        }

        public static List<Concepto> BC3ToList(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.GetEncoding("iso-8859-1"));

            List<Concepto> res = new List<Concepto>();

            string line;

            for (int i = 0; i < lines.Length; i++)
            {
                // Forzamos el archivo a un formato estandar para trabajar
                line = lines[i];
                if (lines[i].StartsWith("~"))
                {
                    while (i < lines.Length)
                    {
                        i++;
                        if (i == lines.Length)
                        {
                            break;
                        }
                        if (lines[i].StartsWith("~"))
                        {
                            i--;
                            break;
                        }
                        else
                        {
                            line += lines[i];
                        }
                    }
                }

                // Transformamo la linea en conceptos
                if (line.StartsWith("~C"))
                {
                    res = bc3LineToConcept.ConceptLine(line, res);
                }
                else if (line.StartsWith("~T"))
                {
                    res = bc3LineToConcept.TextLine(line, res);
                }
                else if (line.StartsWith("~D"))
                {
                    res = bc3LineToConcept.DescompositionLine(line, res);
                }
            }

            return res;
        }

        public static async Task BC3ToJson(string filePath, string filename)
        {
            List<Concepto> res = BC3ToList(filePath);

            string name = $"{filename}.json";
            await using FileStream createStream = File.Create(name);
            await JsonSerializer.SerializeAsync(createStream, res);
        }
    }
}

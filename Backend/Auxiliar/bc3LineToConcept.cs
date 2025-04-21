using Bc3_WPF.backend.Modelos;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bc3_WPF.backend.Auxiliar
{
    internal class bc3LineToConcept
    {
        public static List<Concepto> ConceptLine(string line,
            List<Concepto> res)
        {
            string descompuesto = line.Substring(3);
            string[] atributos = descompuesto.Split('|');

            if (!Concepto.searchConcepto(res, atributos[0]))
            {
                float precio = 0;
                if (atributos[3] != "")
                    precio = float.Parse(atributos[3], CultureInfo.InvariantCulture.NumberFormat);
                DateOnly? fecha = Parse.ParseDate(atributos[4]);
                Concepto concepto = new Concepto
                {
                    Id = atributos[0],
                    medidor = atributos[1],
                    name = atributos[2],
                    precio = precio,
                    fecha = fecha
                };

                res.Add(concepto);
            }
            return res;
        }

        public static List<Concepto> TextLine(string line,
            List<Concepto> res)
        {
            string descompuesto = line.Substring(3);
            string[] atributos = descompuesto.Split('|');

            string name = atributos[0];
            string description = atributos[1];
            Concepto concepto = res.FirstOrDefault(c => c.Id == name);

            if (concepto != null)
            {
                int index = res.IndexOf(concepto);
                concepto.description = description;
                res[index] = concepto;
            }

            return res;
        }

        public static List<Concepto> DescompositionLine(string line,
            List<Concepto> res)
        {
            string[] atributos = line.Substring(3).Split('|');

            string id = atributos[0];
            string descomposiciones = atributos[1];
            Concepto concepto = res.Where(e => e.Id.Split("#")[0].Split("\\")[0] == id.Split("#")[0].Split("\\")[0]).First();

            if (concepto != null)
            {
                int index = res.IndexOf(concepto);
                List<KeyValuePair<string, float?>> desc = getDescomposicion(descomposiciones);

                concepto.descomposicion = desc;
                res[index] = concepto;
            }
            return res;
        }

        private static List<KeyValuePair<string, float?>> getDescomposicion(
            string descomposiciones)
        {
            List<KeyValuePair<string, float?>> res = new List<KeyValuePair<string, float?>>();

            string[] pieces = descomposiciones.Split("\\");
            string id;

            for (int i = 0; i < pieces.Length - 1; i++)
            {
                id = pieces[i];
                i = i + 2;
                var value = pieces[i];
                KeyValuePair<string, float?> key;
                if (value != "")
                {
                    key = new KeyValuePair<string, float?>(id, float.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
                }
                else
                {
                    key = new KeyValuePair<string, float?>(id, null);
                }

                res.Add(key);
            }

            return res;
        }
    }
}

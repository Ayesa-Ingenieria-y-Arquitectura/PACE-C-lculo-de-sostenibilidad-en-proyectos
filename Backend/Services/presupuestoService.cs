using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Auxiliar;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Xml;

namespace Bc3_WPF.backend.Services
{

    public class presupuestoService
    {
        public static Presupuesto loadFromBC3(string filename)
        {
            List<Concepto> lectura = Parse.BC3ToList(filename);
            Presupuesto presupuesto = presupuestoFromConcept(lectura);

            return presupuesto;
        }

        public static Presupuesto loadFromJson(string filename)
        {
                string json = File.ReadAllText(filename, System.Text.Encoding.GetEncoding("iso-8859-1"));
                Presupuesto obj = JsonSerializer.Deserialize<Presupuesto>(json);
                return obj;
        }

        private static Presupuesto presupuestoFromConcept(List<Concepto> conceptos)
        {
            Concepto principal = searchPrincipal(conceptos);
            Presupuesto res = new Presupuesto { Id = principal.Id, name = principal.name};

            if (principal.fecha != null)
                res.fecha = (DateOnly)principal.fecha;

            if (principal.descomposicion != null)
            {
                res.hijos = getHijos(conceptos, principal.descomposicion);
            }
            return res;

        }

        private static List<Presupuesto>? getHijos(List<Concepto> conceptos, List<KeyValuePair<string, float?>> hijos)
        {
            List<Presupuesto> res = new List<Presupuesto>();

            foreach (var h in hijos)
            {
                Concepto principal = searchById(conceptos, h.Key);

                Presupuesto hijo = new Presupuesto
                {
                    Id = principal.Id,
                    name = principal.name,
                    quantity = h.Value
                };

                if (principal.fecha != null)
                    hijo.fecha = (DateOnly)principal.fecha;

                if (principal.descomposicion != null)
                {
                    hijo.hijos = getHijos(conceptos, principal.descomposicion);
                }
                res.Add(hijo);
            }

            return res;
        }

        private static Concepto searchById(List<Concepto> conceptos, string id)
        {
            Concepto res = conceptos.Where(e => e.Id.Split("#")[0].Split("\\")[0] == id.Split("#")[0].Split("\\")[0]).First();

            return res;
        }

        private static Concepto searchPrincipal(List<Concepto> conceptos)
        {
            Concepto res = conceptos.Find(e => e.Id.EndsWith("##"));

            return res;
        }

        public static void saveJson(string filePath, Presupuesto presupuesto)
        {
            if (presupuesto != null)
            {
                string json = JsonSerializer.Serialize(presupuesto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            else
            {
                throw new NullReferenceException("No existe un presupuesto a descargar");
            }
        }

        public static List<string> getConceptsSinHijos(string filePath)
        {
            List<Concepto> lectura = Parse.BC3ToList(filePath);
            List<string> res = lectura
            .Where(c => c.descomposicion == null || c.descomposicion.Count == 0)
            .Select(c => c.Id)
            .ToList();

            return res;
        }
    }

}

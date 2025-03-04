using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Auxiliar;
using System.IO;
using System.Text.Json;

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
            try
            {
                string json = File.ReadAllText(filename, System.Text.Encoding.GetEncoding("iso-8859-1"));
                Presupuesto obj = JsonSerializer.Deserialize<Presupuesto>(json);
                return obj;
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
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
            Concepto res = conceptos.Where(e => e.Id == id).First();

            return res;
        }

        private static Concepto searchPrincipal(List<Concepto> conceptos)
        {
            Concepto res = conceptos.Find(e => e.Id.EndsWith("##"));

            return res;
        }
    }

}

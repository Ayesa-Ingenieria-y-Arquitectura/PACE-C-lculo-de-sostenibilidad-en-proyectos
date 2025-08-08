using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Auxiliar;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Xml;
using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;
using System.Security.AccessControl;
using System.Collections.Generic;

namespace Bc3_WPF.backend.Services
{

    public class presupuestoService
    {
        public static (Presupuesto, HashSet<string>, Dictionary<string, List<string>>) loadFromBC3(string filename)
        {
            List<SustainabilityRecord> data = SustainabilityService.getFromDatabase();

            List<Concepto> lectura = Parse.BC3ToList(filename);
            Presupuesto presupuesto = presupuestoFromConcept(lectura);
            presupuesto = fillPresupuesto(presupuesto, data);

            HashSet<string> list = SustainabilityService.medidores(data);
            Dictionary<string, List<string>> dict = SustainabilityService.getFromCategories(data);

            return (presupuesto, list, dict);
        }

        public static (Presupuesto, HashSet<string>, Dictionary<string, List<string>>) loadFromJson(string filename)
        {
            List<SustainabilityRecord> data = SustainabilityService.getFromDatabase();

            string json = File.ReadAllText(filename, System.Text.Encoding.GetEncoding("iso-8859-1"));
            Presupuesto obj = JsonSerializer.Deserialize<Presupuesto>(json);
            obj = fillPresupuesto(obj, data);

            HashSet<string> list = SustainabilityService.medidores(data);
            Dictionary<string, List<string>> dict = SustainabilityService.getFromCategories(data);

            return (obj, list, dict);
        }

        private static Presupuesto presupuestoFromConcept(List<Concepto> conceptos)
        {
            Concepto principal = searchPrincipal(conceptos);
            Presupuesto res = new Presupuesto { Id = principal.Id, name = principal.name };

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
            Concepto res = conceptos.Find(e => e.Id.Contains("##"));

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

        public static Presupuesto fillPresupuesto(Presupuesto presupuesto, List<SustainabilityRecord> data)
        {
            List<Presupuesto> prs = new();

            foreach (Presupuesto hijo in presupuesto.hijos)
            {
                Presupuesto p = hijo;

                if (hijo.hijos != null)
                {
                    p = fillPresupuesto(hijo, data);
                }
                else
                {
                    List<SustainabilityRecord> sr = data.Where(r => r.ExternalId == hijo.Id).ToList();
                    p.values = new();
                    p.factores = new();
                    p.medidores = new();
                    foreach (SustainabilityRecord s in sr)
                    {
                        p.InternalId = s.InternalId;
                        p.category = s.Category;
                        p.medidores.Add(s.Indicator);
                        p.values.Add(s.Value);
                        p.factores.Add(s.Factor);
                    }
                }

                prs.Add(p);
            }

            presupuesto.hijos = prs;
            return presupuesto;
        }

        public static Presupuesto FindPresupuestoById(Presupuesto presupuesto, string id)
        {
            // Base case: if the current Presupuesto's Id matches the search Id
            if (presupuesto.Id == id)
            {
                return presupuesto;
            }

            // Recursively search through the hijos (children)
            if(presupuesto.hijos != null)
            {
                foreach (var hijo in presupuesto.hijos)
                {
                    var result = FindPresupuestoById(hijo, id);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }else
            {
                return null;
            }


            return null;
        }

        public static List<Presupuesto> toArray(Presupuesto p)
        {
            List<Presupuesto> res = [p];

            if (p.hijos != null && p.hijos.Count != 0)
            {
                foreach (Presupuesto hijo in p.hijos)
                {
                    List<Presupuesto> aux = toArray(hijo);
                    res.AddRange(aux);
                }
            }

            return res;
        }
    }
}

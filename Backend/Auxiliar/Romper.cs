using Bc3_WPF.backend.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Bc3_WPF.Backend.Auxiliar
{
    public class Romper
    {

        public static Presupuesto change(Presupuesto original,
            List<KeyValuePair<string, List<Presupuesto>>> historial,
            List<Presupuesto> cambios, string Id, Boolean first)
        {
            List<Presupuesto> hijos = original.hijos;
            Presupuesto og;

            if (historial.Count > 0)
            {
                for (int i = 0; i < historial.Count; i++)
                {
                    hijos = hijos.Where(e => e.Id == historial[i].Key).First().hijos;
                }

                if (first)
                {
                    og = hijos.Where(e => e.Id == Id).First();
                    if (og.quantity != cambios.Sum(o => o.quantity))
                    {
                        throw new ArgumentException("Las cantidades no coinciden");
                    }
                    else
                    {
                        hijos.Remove(hijos.Where(e => e.Id == Id).First());
                        hijos.AddRange(cambios);
                        hijos.Sort((e, a) => e.Id.CompareTo(a.Id));
                    }
                }
                else
                {
                    Presupuesto p = hijos.Where(e => e.Id == Id).First();
                    int ind = hijos.IndexOf(p);
                    p.hijos = cambios;
                    hijos[ind] = p;
                }

                string lastId = historial[historial.Count -1].Key;

                List<KeyValuePair<string, List<Presupuesto>>> h = historial;
                h.Remove(historial[historial.Count - 1]);

                return change(original, h, hijos, lastId, false);

                
            }
            else
            {
                if (first)
                {
                    og = hijos.Where(e => e.Id == Id).First();
                    if (og.quantity != cambios.Sum(o => o.quantity))
                    {
                        throw new ArgumentException("Las cantidades no coinciden");
                    }
                    else
                    {
                        hijos.Remove(hijos.Where(e => e.Id == Id).First());
                        hijos.AddRange(cambios);
                    }
                }
                else
                {
                    Presupuesto h = hijos.Where(e => e.Id == Id).First();
                    int ind = hijos.IndexOf(h);
                    h.hijos = cambios;
                    hijos[ind] = h;
                }
                original.hijos = hijos;

                return original;
            }


        }
    }
}

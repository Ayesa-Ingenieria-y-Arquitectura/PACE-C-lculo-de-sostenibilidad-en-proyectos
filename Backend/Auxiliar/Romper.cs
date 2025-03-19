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
            List<string> h1,
            List<Presupuesto> cambios, string Id, Boolean first)
        {
            List<Presupuesto> hijos = original.hijos;
            Presupuesto og;

            if (h1.Count > 0)
            {
                for (int i = 0; i < h1.Count; i++)
                {
                    hijos = hijos.Where(e => e.Id == h1[i]).First().hijos;
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

                string lastId = h1[h1.Count -1];

                List<string> h = h1;
                h.Remove(h1[h1.Count - 1]);

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

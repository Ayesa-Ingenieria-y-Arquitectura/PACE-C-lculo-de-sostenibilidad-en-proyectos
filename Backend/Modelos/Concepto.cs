namespace Bc3_WPF.backend.Modelos
{
    public class Concepto
    {
        public string Id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public List<KeyValuePair<string, float?>>? descomposicion { get; set; }
        public float? precio { get; set; }
        public DateOnly? fecha { get; set; }
        public string? medidor { get; set; }


        public override string ToString()
        {
            string aux = "";
            if (descomposicion != null)
            {
                foreach (KeyValuePair<string, float?> k in descomposicion)
                {
                    aux += $"{k.Key}, ";
                }
            }
            return $"id:{Id}, name:{name}, description:{description}, descomposition:[{aux}]," +
                $" precio:{precio}, fecha:{fecha}, medidor:{medidor}";
        }

        public static bool searchConcepto(List<Concepto> conceptos, string id)
        {
            bool res = true;
            List<string> list = conceptos.Select(x => x.Id).ToList();

            if (!list.Contains(id))
            {
                res = false;
            }

            return res;
        }
    }
}

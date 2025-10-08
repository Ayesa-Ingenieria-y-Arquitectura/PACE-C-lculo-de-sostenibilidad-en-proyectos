using System.Collections.ObjectModel;
using Bc3_WPF.backend.Modelos;
using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.Backend.Services;

namespace Bc3_WPF.Screens.Tabla_Presupuestos
{
    public partial class TablaDePresupuestos : System.Windows.Controls.UserControl
    {
        private Presupuesto? presupuesto;
        private List<string> historial = new();
        private List<Presupuesto> currentData = new();
        private List<Presupuesto> showing = new();
        private List<KeyValuePair<string, Presupuesto>> previous = new();
        private List<KeyValuePair<string, List<KeyValuePair<string, decimal>>>> changes = new();
        private ObservableCollection<Presupuesto> treeInfo = new();
        private Dictionary<string, List<string>> idArray = new();
        private List<string> medidores = new();
        private string med = "";
        private int pageNumber = 1;
        private int rowsPerPage = 20;
        private decimal? pages;
        private string? fileName;
        private string? path;
        private string dbSelected = "DB1";
        private Presupuesto? currentSelectedItem;

        // Campos para la funcionalidad de búsqueda
        private List<Presupuesto> _filteredData = new();
        private string _lastSearchText = string.Empty;

        // Nuevo campo para almacenar los términos de búsqueda activos
        private HashSet<string> _searchTerms = new HashSet<string>();

        private List<Material> _materials = new();

        public TablaDePresupuestos()
        {
            InitializeComponent();

            // Inicializar campos
            _searchTerms = new HashSet<string>();
            _filteredData = new List<Presupuesto>();

            List<SustainabilityRecord> aux = SustainabilityService.getFromDatabase();
            _materials = SustainabilityService.getMaterials(aux);
        }

    }
}
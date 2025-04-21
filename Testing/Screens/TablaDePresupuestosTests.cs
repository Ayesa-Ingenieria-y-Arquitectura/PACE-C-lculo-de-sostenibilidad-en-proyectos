using Bc3_WPF.backend.Modelos;
using Bc3_WPF.Backend.Modelos;
using Bc3_WPF.backend.Services;
using Bc3_WPF.Backend.Services;
using Bc3_WPF.Screens;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Testing.Screens
{
    [TestFixture]
    // Clase wrapper para pruebas de métodos estáticos
    public class SustainabilityServiceWrapper
    {
        private List<SustainabilityRecord> _testRecords;
        private List<KeyValuePair<string, string>> _testCodeRelations;

        public void SetTestData(List<SustainabilityRecord> records, List<KeyValuePair<string, string>> codeRelations)
        {
            _testRecords = records;
            _testCodeRelations = codeRelations;

            // Reemplazar los métodos estáticos usando reflexión
            typeof(SustainabilityService).GetField("_cachedRecords", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, records);

            // Alternativamente, puedes usar hooks para interceptar llamadas estáticas
            // con bibliotecas como Microsoft Fakes, Typemock Isolator o similares
        }
    }

    // Clase wrapper para pruebas de presupuestoService
    public class PresupuestoServiceWrapper
    {
        private Dictionary<Tuple<Presupuesto, string>, Presupuesto> _findResults = new Dictionary<Tuple<Presupuesto, string>, Presupuesto>();

        public void SetupFindPresupuestoById(Presupuesto root, string id, Presupuesto result)
        {
            _findResults[Tuple.Create(root, id)] = result;

            // Podemos usar TypeMock o Microsoft Fakes para interceptar la llamada estática real
            // O usamos reflexión para establecer un campo estático que guarde estos resultados
            // O creamos un delegado que se usará en las pruebas

            // Por ejemplo, podrías hacer algo como esto:
            var methodInfo = typeof(presupuestoService).GetMethod("FindPresupuestoById", BindingFlags.Public | BindingFlags.Static);
            if (methodInfo != null)
            {
                // Aquí necesitaríamos usar una biblioteca como Harmony, Detours o similar para parchear el método
                // Este es solo un esquema conceptual
                /* 
                MethodRedirection.Redirect(methodInfo, (Func<Presupuesto, string, Presupuesto>)((rootNode, searchId) => {
                    var key = Tuple.Create(rootNode, searchId);
                    if (_findResults.ContainsKey(key))
                        return _findResults[key];
                    return null;
                }));
                */
            }
        }
    }

    public class TablaDePresupuestosTests
    {
        private TablaDePresupuestos _tablaDePresupuestos;
        private PresupuestoServiceWrapper _presupuestoServiceWrapper;

        [SetUp]
        public void Setup()
        {
            // Verificar que estamos en un subproceso STA
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Assert.Fail("Esta prueba debe ejecutarse en un subproceso STA.");
            }

            // Inicializar los wrappers para servicios estáticos
            _presupuestoServiceWrapper = new PresupuestoServiceWrapper();

            // Crear instancia de la clase a probar
            _tablaDePresupuestos = new TablaDePresupuestos();

            // Inicializar la infraestructura de WPF si es necesario
            if (Application.Current == null)
            {
                new Application(); // Crea una instancia de la aplicación WPF para el contexto
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void NavigateToChildren_UpdatesCurrentDataAndHistory()
        {
            // Arrange
            var parent = new Presupuesto { Id = "Parent1", name = "Parent Item" };
            var child1 = new Presupuesto { Id = "Child1", name = "Child Item 1" };
            var child2 = new Presupuesto { Id = "Child2", name = "Child Item 2" };
            parent.hijos = new List<Presupuesto> { child1, child2 };

            // Obtener acceso a campos privados mediante reflexión
            var fieldInfo = typeof(TablaDePresupuestos).GetField("presupuesto", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(_tablaDePresupuestos, parent);

            var currentDataField = typeof(TablaDePresupuestos).GetField("currentData", BindingFlags.NonPublic | BindingFlags.Instance);
            var historialField = typeof(TablaDePresupuestos).GetField("historial", BindingFlags.NonPublic | BindingFlags.Instance);

            // Establecer historial inicial
            historialField.SetValue(_tablaDePresupuestos, new List<string>());

            // Act - Invocar método privado mediante reflexión
            MethodInfo method = typeof(TablaDePresupuestos).GetMethod("NavigateToChildren",
                                  BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_tablaDePresupuestos, new object[] { parent });

            // Assert
            var updatedCurrentData = (List<Presupuesto>)currentDataField.GetValue(_tablaDePresupuestos);
            var updatedHistorial = (List<string>)historialField.GetValue(_tablaDePresupuestos);

            Assert.AreEqual(2, updatedCurrentData.Count);
            Assert.Contains("Parent1", updatedHistorial);
            Assert.AreEqual(1, updatedHistorial.Count);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ChangePage_UpdatesShowingCollectionCorrectly()
        {
            // Arrange
            var items = new List<Presupuesto>();
            for (int i = 1; i <= 25; i++)
            {
                items.Add(new Presupuesto { Id = $"Item{i}", name = $"Item {i}" });
            }

            // Configurar campos privados
            var currentDataField = typeof(TablaDePresupuestos).GetField("currentData", BindingFlags.NonPublic | BindingFlags.Instance);
            currentDataField.SetValue(_tablaDePresupuestos, items);

            var pageNumberField = typeof(TablaDePresupuestos).GetField("pageNumber", BindingFlags.NonPublic | BindingFlags.Instance);
            pageNumberField.SetValue(_tablaDePresupuestos, 1);

            var rowsPerPageField = typeof(TablaDePresupuestos).GetField("rowsPerPage", BindingFlags.NonPublic | BindingFlags.Instance);
            rowsPerPageField.SetValue(_tablaDePresupuestos, 20);

            var showingField = typeof(TablaDePresupuestos).GetField("showing", BindingFlags.NonPublic | BindingFlags.Instance);
            showingField.SetValue(_tablaDePresupuestos, items.Take(20).ToList());

            // Act
            MethodInfo method = typeof(TablaDePresupuestos).GetMethod("ChangePage",
                                 BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_tablaDePresupuestos, new object[] { 1 });

            // Assert
            var updatedPageNumber = (int)pageNumberField.GetValue(_tablaDePresupuestos);
            var updatedShowing = (List<Presupuesto>)showingField.GetValue(_tablaDePresupuestos);

            Assert.AreEqual(2, updatedPageNumber);
            Assert.AreEqual(5, updatedShowing.Count);
            Assert.AreEqual("Item21", updatedShowing[0].Id);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ProcessValidSplitData_UpdatesPresupuestoStructureCorrectly()
        {
            // Arrange
            var rootItem = new Presupuesto { Id = "Root", name = "Root Item" };
            var itemToSplit = new Presupuesto
            {
                Id = "ItemToSplit",
                name = "Item to Split",
                quantity = 100,
                category = "Category1"
            };
            rootItem.hijos = new List<Presupuesto> { itemToSplit };

            var splitItems = new List<Presupuesto> {
                new Presupuesto {
                    Id = "SplitItem1",
                    name = "Split Item 1",
                    quantity = 40,
                    category = "Category1"
                },
                new Presupuesto {
                    Id = "SplitItem2",
                    name = "Split Item 2",
                    quantity = 60,
                    category = "Category1"
                }
            };

            // Configurar campos privados
            var presupuestoField = typeof(TablaDePresupuestos).GetField("presupuesto", BindingFlags.NonPublic | BindingFlags.Instance);
            presupuestoField.SetValue(_tablaDePresupuestos, rootItem);

            var historialField = typeof(TablaDePresupuestos).GetField("historial", BindingFlags.NonPublic | BindingFlags.Instance);
            historialField.SetValue(_tablaDePresupuestos, new List<string>());

            var previousField = typeof(TablaDePresupuestos).GetField("previous", BindingFlags.NonPublic | BindingFlags.Instance);
            previousField.SetValue(_tablaDePresupuestos, new List<KeyValuePair<string, Presupuesto>>());

            // Configurar Romper.change para simular la división
            // Esto depende de cómo esté implementado Romper.change

            // Act
            MethodInfo method = typeof(TablaDePresupuestos).GetMethod("ProcessValidSplitData",
                                 BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_tablaDePresupuestos, new object[] { itemToSplit, "Root", splitItems });

            // Assert
            var updatedPrevious = (List<KeyValuePair<string, Presupuesto>>)previousField.GetValue(_tablaDePresupuestos);
            Assert.AreEqual(1, updatedPrevious.Count);
            Assert.AreEqual("Root", updatedPrevious[0].Key);
            Assert.AreEqual("ItemToSplit", updatedPrevious[0].Value.Id);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void IsValidSplitData_ReturnsTrueWhenQuantitiesMatch()
        {
            // Arrange
            var original = new Presupuesto { quantity = 100 };
            var splitData = new List<Presupuesto> {
                new Presupuesto { quantity = 40 },
                new Presupuesto { quantity = 60 }
            };

            // Act
            MethodInfo method = typeof(TablaDePresupuestos).GetMethod("IsValidSplitData",
                                 BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (bool)method.Invoke(_tablaDePresupuestos, new object[] { original, splitData });

            // Assert
            Assert.IsTrue(result);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void IsValidSplitData_ReturnsFalseWhenQuantitiesDontMatch()
        {
            // Arrange
            var original = new Presupuesto { quantity = 100 };
            var splitData = new List<Presupuesto> {
                new Presupuesto { quantity = 40 },
                new Presupuesto { quantity = 50 }
            };

            // Act
            MethodInfo method = typeof(TablaDePresupuestos).GetMethod("IsValidSplitData",
                                 BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (bool)method.Invoke(_tablaDePresupuestos, new object[] { original, splitData });

            // Assert
            Assert.IsFalse(result);
        }

        [TearDown]
        public void Cleanup()
        {
            // Limpiar recursos si es necesario
            _tablaDePresupuestos = null;
            _presupuestoServiceWrapper = null;

            // Restablecer valores estáticos si es necesario
            typeof(SustainabilityService).GetField("_cachedRecords", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);

            // Restaurar el comportamiento original de presupuestoService si fue modificado
            // Esto dependerá de cómo implementes la sobrescritura de métodos estáticos
        }
    }
}

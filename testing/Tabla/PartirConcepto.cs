using Bc3_WPF.backend.Modelos;
using Bc3_WPF.backend.Auxiliar;
using Bc3_WPF.Backend.Auxiliar;
using System.Text.Json;

namespace testing.Tabla
{
    public class PartirConcepto
    {
        public Presupuesto p = new Presupuesto
        {
            Id = "Raiz",
            name = "Raiz",
            hijos = [
                new Presupuesto{
                    Id = "1",
                    name = "1",
                    quantity = 2
                },
                new Presupuesto{
                    Id="2",
                    name = "2",
                    quantity = 1
                }
                ]
        };

        List<Presupuesto> cambios = [new Presupuesto { Id="3", name="3", quantity=1 },
            new Presupuesto { Id = "4", name = "4", quantity = 1 }];

        public Presupuesto s = new Presupuesto
        {
            Id = "Raiz",
            name = "Raiz",
            hijos = [
                new Presupuesto{
                    Id="2",
                    name = "2",
                    quantity = 1
                },
                new Presupuesto { 
                    Id="3",
                    name="3",
                    quantity=1 },
                new Presupuesto { 
                    Id = "4", 
                    name = "4", 
                    quantity = 1 }
               ]
        };

        [Fact]
        public void makePartition()
        {
            Presupuesto res = Romper.change(p, [], cambios, "1", true);

            Assert.Equal(JsonSerializer.Serialize(res), JsonSerializer.Serialize(s));
        }

        [Fact]
        public void Badquantity()
        {
            Assert.Throws<ArgumentException>(() => Romper.change(p, [], cambios, "2", true));
        }

        [Fact]
        public void nullPresupuesto()
        {
            Assert.Throws<NullReferenceException>(() => Romper.change(null, [], cambios, "2", true));
        }

        [Fact]
        public void nullHistorial()
        {
            Assert.Throws<NullReferenceException>(() => Romper.change(p, null, cambios, "2", true));
        }

        [Fact]
        public void nullCambios()
        {
            Assert.Throws<ArgumentNullException>(() => Romper.change(p, [], null, "2", true));
        }

        [Fact]
        public void emptyCambios()
        {
            Assert.Throws<ArgumentException>(() => Romper.change(p, [], [], "2", true));
        }

        [Fact]
        public void badFirst()
        {
            Presupuesto res = Romper.change(p, [], cambios, "1", false);

            Assert.NotEqual(JsonSerializer.Serialize(res), JsonSerializer.Serialize(s));
        }

        [Fact]
        public void nullId()
        {
            Assert.Throws<InvalidOperationException>(() => Romper.change(p, [], cambios, null, true));
        }

        [Fact]
        public void emptyId()
        {
            Assert.Throws<InvalidOperationException>(() => Romper.change(p, [], cambios, "", true));
        }

        [Fact]
        public void badId()
        {
            Assert.Throws<InvalidOperationException>(() => Romper.change(p, [], cambios, "3", true));
        }
    }
}

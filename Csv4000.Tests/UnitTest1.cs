using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Csv4000.Tests
{
    public class MyModelExemple
    {
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public DateTime DateValue { get; set; }
        public string StringValue { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestAsync()
        {
            CsvOf<MyModelExemple> sut = createSut();
            sut.Clear();

            var count = new Random().Next(1, 10);
            for (int i = 0; i < count; i++)
                await sut.WriteAsync(
                    new MyModelExemple
                    {
                        StringValue = "testando " + i,
                        DateValue = DateTime.Now,
                        FloatValue = 1.5f * i,
                        IntValue = 5 * i
                    }
                );

            var result = await sut.ReadAsync();
            Assert.AreEqual(count, result.Count());
        }

        private CsvOf<MyModelExemple> createSut()
        {
            return new CsvOf<MyModelExemple>("test.csv");
        }

        [TestMethod]
        public void Test()
        {
            var sut = createSut();
            sut.Clear();

            var count = new Random().Next(1, 10);
            for (int i = 0; i < count; i++)
                sut.Write(
                    new MyModelExemple
                    {
                        StringValue = "testando " + i,
                        DateValue = DateTime.Now,
                        FloatValue = 1.5f * i,
                        IntValue = 5 * i
                    }
                );

            var result = sut.Read();
            Assert.AreEqual(count, result.Count());
        }

        [TestMethod]
        public void StringValueContainingComma()
        {
            var sut = createSut();
            sut.Clear();

                sut.Write(
                    new MyModelExemple
                    {
                        StringValue = "testando;123"
                    }
                );

            var result = sut.Read().First();
            Assert.AreEqual("testando;123", result.StringValue);
        }
    }
}

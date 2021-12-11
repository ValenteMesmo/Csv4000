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
        public bool? NullableBool { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        private CsvOf<MyModelExemple> createSut()
        {
            return new CsvOf<MyModelExemple>($"test-{DateTime.Now.ToString("hh.mm.ss.fff")}-{DateTime.Now.Minute}-{DateTime.Now.Second}-{DateTime.Now.Millisecond}.csv");
        }

        [TestMethod]
        public async Task TestAsync()
        {
            CsvOf<MyModelExemple> sut = createSut();
            

                await sut.WriteAsync(
                    new MyModelExemple
                    {
                        NullableBool = true 
                    }
                );

            var result = await sut.ReadAsync();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(true, result.First().NullableBool);
            sut.Clear();
        }

        [TestMethod]
        public void Test()
        {
            var sut = createSut();

            var count = 10;
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
            sut.Clear();
        }

        [TestMethod]
        public void StringValueContainingComma()
        {
            var sut = createSut();

            sut.Write(
                new MyModelExemple
                {
                    StringValue = "testando;123"
                }
            );

            var result = sut.Read().First();
            Assert.AreEqual("testando;123", result.StringValue);
            sut.Clear();
        }

        [TestMethod]
        public void StringValueSpecialCharacter()
        {
            var sut = createSut();

            sut.Write(
                new MyModelExemple
                {
                    StringValue = "praça;123"
                }
            );

            var result = sut.Read().First();
            Assert.AreEqual("praça;123", result.StringValue);
            sut.Clear();
        }
    }
}

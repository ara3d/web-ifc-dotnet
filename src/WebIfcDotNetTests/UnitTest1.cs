using WebIfcClrWrapper;

namespace WebIfcDotNetTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var r = new ThisIsAManagedClass();
            Console.WriteLine(r.MyString);
        }
    }
} 
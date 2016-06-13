using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Datakit.Abstractions.Tests
{
    [TestFixture]
    public class TestBranch
    {
        [Test]
        public void TestGetHead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.ReadAll(It.IsAny<List<string>>())).Returns(new Task<string>(() => "test"));
            var b = new Branch(mock.Object, "master");
            Assert.That(b.Head, Is.EqualTo("test"));
        }

        [Test]
        public void TestGetPath()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Mkdir(It.IsAny<List<string>>()));
            var b = new Branch(mock.Object, "master");
            Assert.That(b.Path, Is.EquivalentTo(new List<string> {"branch", "master"}));
            var expectedPath = new List<string> {"branch", "master"};
            mock.Verify(c => c.Mkdir(It.Is<List<string>>(l => l.SequenceEqual(expectedPath))), Times.Once);
        }

        [Test]
        public void TestRead()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.ReadAll(It.IsAny<List<string>>())).Returns(new Task<string>(() => "test"));
            var b = new Branch(mock.Object, "master");
            var result = b.Read(new List<string> {"foo", "bar"});

            var expectedPath = new List<string> {"branch", "master", "ro", "foo", "bar"};
            mock.Verify(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(expectedPath))));
            Assert.That(result, Is.EqualTo("test"));
        }
    }
}
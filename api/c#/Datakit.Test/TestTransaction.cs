using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Datakit.Abstractions.Tests
{
    [TestFixture]
    public class TestTransaction
    {
        private readonly List<string> _basePath = new List<string> {"branch", "master", "transactions", "test"};

        [Test]
        public async Task TestClose()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Mkdir(It.IsAny<List<string>>()));
            mock.Setup(c => c.Write(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()));
            var b = new Branch(mock.Object, "master");
            var t = await b.NewTransaction("test");
            await t.Close();

            var expectedPath = _basePath.ToList();
            expectedPath.Add("ctl");
            mock.Verify(c => c.Write(It.Is<List<string>>(l => l.SequenceEqual(expectedPath)), "close", false));
        }

        [Test]
        public async Task TestCommit()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Mkdir(It.IsAny<List<string>>()));
            mock.Setup(c => c.Write(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()));
            var b = new Branch(mock.Object, "master");
            var t = await b.NewTransaction("test");
            await t.Commit();

            var expectedPath = _basePath.ToList();
            expectedPath.Add("ctl");
            mock.Verify(c => c.Write(It.Is<List<string>>(l => l.SequenceEqual(expectedPath)), "commit", false));
        }

        [Test]
        public async Task TestGetPath()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Mkdir(It.IsAny<List<string>>()));
            var b = new Branch(mock.Object, "master");
            var t = await b.NewTransaction("test");
            Assert.That(t.Path, Is.EquivalentTo(_basePath));
            mock.Verify(c => c.Mkdir(It.Is<List<string>>(l => l.SequenceEqual(_basePath))), Times.Once);
        }

        [Test]
        public async Task TestWrite()
        {
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Mkdir(It.IsAny<List<string>>()));
            mock.Setup(c => c.Write(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()));
            var b = new Branch(mock.Object, "master");
            var t = await b.NewTransaction("test");

            await t.Write(new List<string> {"foo"}, "bar");
            await t.Write(new List<string> {"baz", "quux"}, "foobar");

            var path = _basePath.ToList();
            path.Add("rw");
            mock.Verify(c => c.Mkdir(It.Is<List<string>>(l => l.SequenceEqual(path))), Times.Once);
            path.Add("foo");
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(path)),
                "bar", true), Times.Once);

            path = _basePath.ToList();
            path.Add("rw");
            path.Add("baz");
            mock.Verify(c => c.Mkdir(It.Is<List<string>>(l => l.SequenceEqual(path))), Times.Once);
            path.Add("quux");
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(path)),
                "foobar", true), Times.Once);
        }
    }
}
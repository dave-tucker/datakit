using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Datakit.Abstractions.Tests
{
    [TestFixture]
    public class TestWatch
    {
        [Test]
        public async Task TestNext()
        {
            var watch = new List<string> {"branch", "master", "watch", "test.node", "tree.live"};
            var mock = new Mock<IClient>();

            string[] readResults =
            {
                "\n",
                "\n",
                "\n",
                "test",
                "test2"
            };

            var calls = 0;
            mock.Setup(c => c.Open(It.Is<List<string>>(l => l.SequenceEqual(watch)), 0)).Returns(new Task<uint>(() => 1));

            mock.Setup(c => c.Read(1, It.IsAny<ulong>()))
                .Returns(() => new Task<string>( () => readResults[calls]))
                .Callback(() => calls++);

            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var w = await Watch.CreateWatch(mock.Object, b.Path, path);

            var s = await w.Next();
            Assert.That(s.Id, Is.EqualTo("test"));

            mock.Verify(c => c.Open(It.Is<List<string>>(l => l.SequenceEqual(watch)), 0), Times.Once);
            mock.Verify(c => c.Read(1, 0), Times.Once);
            mock.Verify(c => c.Read(1, 1), Times.Once);
            mock.Verify(c => c.Read(1, 2), Times.Once);
            mock.Verify(c => c.Read(1, 3), Times.Once);

            s = await w.Next();
            Assert.That(s.Id, Is.EqualTo("test2"));
            mock.Verify(c => c.Read(1, 7), Times.Once);
        }

        [Test]
        public async Task TestWatchPath()
        {
            var watch = new List<string> {"branch", "master", "watch", "test.node", "tree.live"};
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Open(It.Is<List<string>>(l => l.SequenceEqual(watch)), 0)).Returns(new Task<uint>(() => 1));

            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var w = await Watch.CreateWatch(mock.Object, b.Path, path);
            mock.Verify(c => c.Open(It.Is<List<string>>(l => l.SequenceEqual(watch)), 0), Times.Once);
        }
    }
}
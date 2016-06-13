using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Datakit.Abstractions.Tests
{
    [TestFixture]
    internal class TestRecord
    {
        [Test]
        public async Task TestNoUpgrade()
        {
            var head = new List<string> {"branch", "master", "head"};
            var schema = new List<string> {"snapshots", "test", "ro", "test", "schema-version"};

            var mock = new Mock<IClient>();
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(head)))).Returns(new Task<string>(() => "test"));
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(schema)))).Returns(new Task<string>(() => "7"));
            mock.Setup(c => c.Write(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()));

            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var r = b.NewRecord(path);

            var f1 = new Field<string>(r, "test1", "foo");
            var f2 = new Field<string>(r, "test2", "foo");
            var f3 = new Field<string>(r, "bar/test3", "foo");
            r.Fields.Add(f1);
            r.Fields.Add(f2);
            r.Fields.Add(f3);

            var t1 = new List<string> {"branch", "master", "transactions", "schema_upgrade", "rw", "test", "test1"};
            var t2 = new List<string> {"branch", "master", "transactions", "schema_upgrade", "rw", "test", "test2"};
            var t3 = new List<string>
            {
                "branch",
                "master",
                "transactions",
                "schema_upgrade",
                "rw",
                "test",
                "bar",
                "test3"
            };
            await r.Upgrade(1);

            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t1)),
                "foo", true), Times.Never);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t2)),
                "foo", true), Times.Never);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t3)),
                "foo", true), Times.Never);
        }

        [Test]
        public void TestPath()
        {
            var mock = new Mock<IClient>();
            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var r = b.NewRecord(path);
            Assert.That(r.Path, Is.EquivalentTo(path));
        }

        [Test]
        public async Task TestSync()
        {
            var head = new List<string> {"branch", "master", "head"};
            var p1 = new List<string> {"snapshots", "test", "ro", "test", "test1"};
            var p2 = new List<string> {"snapshots", "test", "ro", "test", "test2"};
            var p3 = new List<string> {"snapshots", "test", "ro", "test", "bar", "test3"};
            var mock = new Mock<IClient>();
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(head)))).Returns(new Task<string>(() => "test"));
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(p1)))).Returns(new Task<string>(() => "foo"));
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(p2)))).Throws(new Exception());
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(p3)))).Throws(new Exception());
            mock.Setup(c => c.Write(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()));

            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var r = b.NewRecord(path);

            var f1 = new Field<string>(r, "test1", "foo");
            var f2 = new Field<string>(r, "test2", "foo");
            var f3 = new Field<string>(r, "bar/test3", "foo");
            r.Fields.Add(f1);
            r.Fields.Add(f2);
            r.Fields.Add(f3);

            var t1 = new List<string> {"branch", "master", "transactions", "defaults", "rw", "test", "test1"};
            var t2 = new List<string> {"branch", "master", "transactions", "defaults", "rw", "test", "test2"};
            var t3 = new List<string> {"branch", "master", "transactions", "defaults", "rw", "test", "bar", "test3"};
            await r.Sync();

            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t1)),
                "foo", true), Times.Never);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t2)),
                "foo", true), Times.Once);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t3)),
                "foo", true), Times.Once);
        }

        [Test]
        public async Task TestUpgrade()
        {
            var head = new List<string> {"branch", "master", "head"};
            var mock = new Mock<IClient>();
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(head)))).Returns(new Task<string>(() => "test"));
            mock.Setup(c => c.Write(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()));

            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var r = b.NewRecord(path);

            var f1 = new Field<string>(r, "test1", "foo");
            var f2 = new Field<string>(r, "test2", "foo");
            var f3 = new Field<string>(r, "bar/test3", "foo");
            r.Fields.Add(f1);
            r.Fields.Add(f2);
            r.Fields.Add(f3);

            var schema = new List<string> { "branch", "master", "transactions", "schema_upgrade", "rw", "test", "schema-version" };
            var t1 = new List<string> {"branch", "master", "transactions", "schema_upgrade", "rw", "test", "test1"};
            var t2 = new List<string> {"branch", "master", "transactions", "schema_upgrade", "rw", "test", "test2"};
            var t3 = new List<string>
            {
                "branch",
                "master",
                "transactions",
                "schema_upgrade",
                "rw",
                "test",
                "bar",
                "test3"
            };
            await r.Upgrade(7);

            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(schema)),
                "7", true), Times.Once);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t1)),
                "foo", true), Times.Once);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t2)),
                "foo", true), Times.Once);
            mock.Verify(c => c.Write(
                It.Is<List<string>>(l => l.SequenceEqual(t3)),
                "foo", true), Times.Once);
        }

        [Test]
        public async void TestWaitForUpdates()
        {
            var head = new List<string> {"branch", "master", "head"};
            var watch = new List<string> {"branch", "master", "watch", "test.node", "tree.live"};
            var schema = new List<string> {"trees", "test", "schema-version"};
            var p1 = new List<string> {"trees", "test", "test1"};
            var watchResult = "test";
            var mock = new Mock<IClient>();
            mock.Setup(c => c.Open(It.Is<List<string>>(l => l.SequenceEqual(watch)), 0)).Returns(new Task<uint>(() => 1));
            mock.Setup(c => c.Read(1, 0)).Returns(new Task<string>(() => watchResult));
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(head)))).Returns(new Task<string>(()=>"test"));
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(schema)))).Returns(new Task<string>(()=>"1"));
            mock.Setup(c => c.ReadAll(It.Is<List<string>>(l => l.SequenceEqual(p1)))).Returns(new Task<string>(()=>"bar"));

            var b = new Branch(mock.Object, "master");
            var path = new List<string> {"test"};
            var r = b.NewRecord(path);

            var field = new Field<string>(r, "test1", "foo");
            r.Fields.Add(field);

            Assert.That(field.RawValue, Is.EqualTo("foo"));
            Assert.That(field.Version, Is.EqualTo(1));

            await r.WaitForUpdates();

            Assert.That(field.RawValue, Is.EqualTo("bar"));
            Assert.That(field.Version, Is.EqualTo(2));
        }
    }
}
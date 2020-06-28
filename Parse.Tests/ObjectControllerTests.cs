using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;

namespace Parse.Tests
{
#warning Finish refactoring.

    [TestClass]
    public class ObjectControllerTests
    {
        ParseClient Client { get; set; }

        [TestInitialize]
        public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestFetch()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { ["__type"] = "Object", ["className"] = "Corgi", ["objectId"] = "st4nl3yW", ["doge"] = "isShibaInu", ["createdAt"] = "2015-09-18T18:11:28.943Z" }));

            return new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData).FetchAsync(new MutableObjectState { ClassName = "Corgi", ObjectId = "st4nl3yW", ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" } }, default, Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "classes/Corgi/st4nl3yW"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("isShibaInu", newState["doge"]);
                Assert.IsFalse(newState.ContainsKey("corgi"));
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestSave()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { ["__type"] = "Object", ["className"] = "Corgi", ["objectId"] = "st4nl3yW", ["doge"] = "isShibaInu", ["createdAt"] = "2015-09-18T18:11:28.943Z" }));

            return new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData).SaveAsync(new MutableObjectState { ClassName = "Corgi", ObjectId = "st4nl3yW", ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" } }, new Dictionary<string, IParseFieldOperation> { ["gogo"] = new Mock<IParseFieldOperation> { }.Object }, default, Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "classes/Corgi/st4nl3yW"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("isShibaInu", newState["doge"]);
                Assert.IsFalse(newState.ContainsKey("corgi"));
                Assert.IsFalse(newState.ContainsKey("gogo"));
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestSaveNewObject()
        {
            MutableObjectState state = new MutableObjectState
            {
                ClassName = "Corgi",
                ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
            };
            Dictionary<string, IParseFieldOperation> operations = new Dictionary<string, IParseFieldOperation> { ["gogo"] = new Mock<IParseFieldOperation> { }.Object };

            Dictionary<string, object> responseDict = new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "Corgi",
                ["objectId"] = "st4nl3yW",
                ["doge"] = "isShibaInu",
                ["createdAt"] = "2015-09-18T18:11:28.943Z"
            };
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Created, responseDict);
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            return controller.SaveAsync(state, operations, default, Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "classes/Corgi"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("isShibaInu", newState["doge"]);
                Assert.IsFalse(newState.ContainsKey("corgi"));
                Assert.IsFalse(newState.ContainsKey("gogo"));
                Assert.AreEqual("st4nl3yW", newState.ObjectId);
                Assert.IsTrue(newState.IsNew);
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestSaveAll()
        {
            List<IObjectState> states = new List<IObjectState>();

            for (int i = 0; i < 30; ++i)
            {
                states.Add(new MutableObjectState
                {
                    ClassName = "Corgi",
                    ObjectId = (i % 2 == 0) ? null : "st4nl3yW" + i,
                    ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
                });
            }

            List<IDictionary<string, IParseFieldOperation>> operationsList = new List<IDictionary<string, IParseFieldOperation>>();

            for (int i = 0; i < 30; ++i)
            {
                operationsList.Add(new Dictionary<string, IParseFieldOperation> { ["gogo"] = new Mock<IParseFieldOperation> { }.Object });
            }

            List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();

            for (int i = 0; i < 30; ++i)
            {
                results.Add(new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, object>
                    {
                        ["__type"] = "Object",
                        ["className"] = "Corgi",
                        ["objectId"] = "st4nl3yW" + i,
                        ["doge"] = "isShibaInu",
                        ["createdAt"] = "2015-09-18T18:11:28.943Z"
                    }
                });
            }

            Dictionary<string, object> responseDict = new Dictionary<string, object> { [nameof(results)] = results };

            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, responseDict);
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            IList<Task<IObjectState>> tasks = controller.SaveAllAsync(states, operationsList, default, Client, CancellationToken.None);

            return Task.WhenAll(tasks).ContinueWith(_ =>
            {
                Assert.IsTrue(tasks.All(task => task.IsCompleted && !task.IsCanceled && !task.IsFaulted));

                for (int i = 0; i < 30; ++i)
                {
                    IObjectState serverState = tasks[i].Result;
                    Assert.AreEqual("st4nl3yW" + i, serverState.ObjectId);
                    Assert.IsFalse(serverState.ContainsKey("gogo"));
                    Assert.IsFalse(serverState.ContainsKey("corgi"));
                    Assert.AreEqual("isShibaInu", serverState["doge"]);
                    Assert.IsNotNull(serverState.CreatedAt);
                    Assert.IsNotNull(serverState.UpdatedAt);
                }

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "batch"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestSaveAllManyObjects()
        {
            List<IObjectState> states = new List<IObjectState>();
            for (int i = 0; i < 102; ++i)
            {
                states.Add(new MutableObjectState
                {
                    ClassName = "Corgi",
                    ObjectId = "st4nl3yW" + i,
                    ServerData = new Dictionary<string, object>
                    {
                        ["corgi"] = "isNotDoge"
                    }
                });
            }
            List<IDictionary<string, IParseFieldOperation>> operationsList = new List<IDictionary<string, IParseFieldOperation>>();

            for (int i = 0; i < 102; ++i)
                operationsList.Add(new Dictionary<string, IParseFieldOperation> { ["gogo"] = new Mock<IParseFieldOperation>().Object });

            // Make multiple response since the batch will be splitted.
            List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();
            for (int i = 0; i < 50; ++i)
            {
                results.Add(new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, object>
                    {
                        ["__type"] = "Object",
                        ["className"] = "Corgi",
                        ["objectId"] = "st4nl3yW" + i,
                        ["doge"] = "isShibaInu",
                        ["createdAt"] = "2015-09-18T18:11:28.943Z"
                    }
                });
            }
            Dictionary<string, object> responseDict = new Dictionary<string, object> { [nameof(results)] = results };
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, responseDict);

            List<IDictionary<string, object>> results2 = new List<IDictionary<string, object>>();
            for (int i = 0; i < 2; ++i)
            {
                results2.Add(new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, object>
                    {
                        ["__type"] = "Object",
                        ["className"] = "Corgi",
                        ["objectId"] = "st4nl3yW" + i,
                        ["doge"] = "isShibaInu",
                        ["createdAt"] = "2015-09-18T18:11:28.943Z"
                    }
                });
            }
            Dictionary<string, object> responseDict2 = new Dictionary<string, object> { [nameof(results)] = results2 };
            Tuple<HttpStatusCode, IDictionary<string, object>> response2 = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, responseDict2);

            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner> { };
            mockRunner.SetupSequence(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response)).Returns(Task.FromResult(response)).Returns(Task.FromResult(response2));

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            IList<Task<IObjectState>> tasks = controller.SaveAllAsync(states, operationsList, default, Client, CancellationToken.None);

            return Task.WhenAll(tasks).ContinueWith(_ =>
            {
                Assert.IsTrue(tasks.All(task => task.IsCompleted && !task.IsCanceled && !task.IsFaulted));

                for (int i = 0; i < 102; ++i)
                {
                    IObjectState serverState = tasks[i].Result;
                    Assert.AreEqual("st4nl3yW" + i % 50, serverState.ObjectId);
                    Assert.IsFalse(serverState.ContainsKey("gogo"));
                    Assert.IsFalse(serverState.ContainsKey("corgi"));
                    Assert.AreEqual("isShibaInu", serverState["doge"]);
                    Assert.IsNotNull(serverState.CreatedAt);
                    Assert.IsNotNull(serverState.UpdatedAt);
                }

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "batch"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestDelete()
        {
            MutableObjectState state = new MutableObjectState
            {
                ClassName = "Corgi",
                ObjectId = "st4nl3yW",
                ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
            };

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, new Dictionary<string, object> { }));

            return new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData).DeleteAsync(state, default, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "classes/Corgi/st4nl3yW"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestDeleteAll()
        {
            List<IObjectState> states = new List<IObjectState>();
            for (int i = 0; i < 30; ++i)
            {
                states.Add(new MutableObjectState
                {
                    ClassName = "Corgi",
                    ObjectId = "st4nl3yW" + i,
                    ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
                });
            }

            List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();

            for (int i = 0; i < 30; ++i)
            {
                results.Add(new Dictionary<string, object> { ["success"] = null });
            }

            Dictionary<string, object> responseDict = new Dictionary<string, object> { [nameof(results)] = results };

            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, responseDict);
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            IList<Task> tasks = controller.DeleteAllAsync(states, default, CancellationToken.None);

            return Task.WhenAll(tasks).ContinueWith(_ =>
            {
                Assert.IsTrue(tasks.All(task => task.IsCompleted && !task.IsCanceled && !task.IsFaulted));

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "batch"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestDeleteAllManyObjects()
        {
            List<IObjectState> states = new List<IObjectState>();
            for (int i = 0; i < 102; ++i)
            {
                states.Add(new MutableObjectState
                {
                    ClassName = "Corgi",
                    ObjectId = "st4nl3yW" + i,
                    ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
                });
            }

            // Make multiple response since the batch will be split.

            List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();

            for (int i = 0; i < 50; ++i)
            {
                results.Add(new Dictionary<string, object> { ["success"] = default });
            }

            Dictionary<string, object> responseDict = new Dictionary<string, object> { [nameof(results)] = results };
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, responseDict);

            List<IDictionary<string, object>> results2 = new List<IDictionary<string, object>>();

            for (int i = 0; i < 2; ++i)
            {
                results2.Add(new Dictionary<string, object> { ["success"] = default });
            }

            Dictionary<string, object> responseDict2 = new Dictionary<string, object> { [nameof(results)] = results2 };
            Tuple<HttpStatusCode, IDictionary<string, object>> response2 = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, responseDict2);

            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.SetupSequence(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response)).Returns(Task.FromResult(response)).Returns(Task.FromResult(response2));

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            IList<Task> tasks = controller.DeleteAllAsync(states, null, CancellationToken.None);

            return Task.WhenAll(tasks).ContinueWith(_ =>
            {
                Assert.IsTrue(tasks.All(task => task.IsCompleted && !task.IsCanceled && !task.IsFaulted));

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "batch"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestDeleteAllFailSome()
        {
            List<IObjectState> states = new List<IObjectState> { };

            for (int i = 0; i < 30; ++i)
            {
                states.Add(new MutableObjectState
                {
                    ClassName = "Corgi",
                    ObjectId = (i % 2 == 0) ? null : "st4nl3yW" + i,
                    ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
                });
            }

            List<IDictionary<string, object>> results = new List<IDictionary<string, object>> { };

            for (int i = 0; i < 15; ++i)
            {
                if (i % 2 == 0)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["error"] = new Dictionary<string, object>
                        {
                            ["code"] = (long) ParseFailureException.ErrorCode.ObjectNotFound,
                            ["error"] = "Object not found."
                        }
                    });
                }
                else
                {
                    results.Add(new Dictionary<string, object> { ["success"] = default });
                }
            }


            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, new Dictionary<string, object> { [nameof(results)] = results }));

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            IList<Task> tasks = controller.DeleteAllAsync(states, null, CancellationToken.None);

            return Task.WhenAll(tasks).ContinueWith(_ =>
            {
                for (int i = 0; i < 15; ++i)
                {
                    if (i % 2 == 0)
                    {
                        Assert.IsTrue(tasks[i].IsFaulted);
                        Assert.IsInstanceOfType(tasks[i].Exception.InnerException, typeof(ParseFailureException));
                        ParseFailureException exception = tasks[i].Exception.InnerException as ParseFailureException;
                        Assert.AreEqual(ParseFailureException.ErrorCode.ObjectNotFound, exception.Code);
                    }
                    else
                    {
                        Assert.IsTrue(tasks[i].IsCompleted);
                        Assert.IsFalse(tasks[i].IsFaulted || tasks[i].IsCanceled);
                    }
                }

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "batch"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectControllerTests))]
        public Task TestDeleteAllInconsistent()
        {
            List<IObjectState> states = new List<IObjectState> { };

            for (int i = 0; i < 30; ++i)
            {
                states.Add(new MutableObjectState
                {
                    ClassName = "Corgi",
                    ObjectId = "st4nl3yW" + i,
                    ServerData = new Dictionary<string, object>
                    {
                        ["corgi"] = "isNotDoge"
                    }
                });
            }

            List<IDictionary<string, object>> results = new List<IDictionary<string, object>> { };

            for (int i = 0; i < 36; ++i)
            {
                results.Add(new Dictionary<string, object> { ["success"] = default });
            }

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.OK, new Dictionary<string, object> { [nameof(results)] = results }));

            ParseObjectController controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);
            IList<Task> tasks = controller.DeleteAllAsync(states, null, CancellationToken.None);

            return Task.WhenAll(tasks).ContinueWith(_ =>
            {
                Assert.IsTrue(tasks.All(task => task.IsFaulted));
                Assert.IsInstanceOfType(tasks[0].Exception.InnerException, typeof(InvalidOperationException));

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "batch"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}

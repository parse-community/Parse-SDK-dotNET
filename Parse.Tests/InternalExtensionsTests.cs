using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Parse.Infrastructure.Utilities;

namespace Parse.Tests
{
    [TestClass]
    public class InternalExtensionsTests
    {
        [TestMethod]
        [Description("Tests that Safe() on a null Task returns a completed task.")]
        public void Safe_NullTask_ReturnsCompletedTask()
        {
            Task nullTask = null;
            var safeTask = nullTask.Safe();
            Assert.IsNotNull(safeTask);
            Assert.IsTrue(safeTask.IsCompletedSuccessfully);
        }

        [TestMethod]
        [Description("Tests that Safe<T>() on a null Task<T> returns a completed task with a default result.")]
        public async Task Safe_NullGenericTask_ReturnsCompletedTaskWithDefaultResult()
        {
            Task<string> nullTask = null;
            var safeTask = nullTask.Safe();
            Assert.IsNotNull(safeTask);
            Assert.IsTrue(safeTask.IsCompletedSuccessfully);
            Assert.IsNull(await safeTask); // Default for string is null.
        }

        [TestMethod]
        [Description("Tests that GetOrDefault returns the correct value when the key exists.")]
        public void GetOrDefault_KeyExists_ReturnsValue()
        {
            var dictionary = new Dictionary<string, int> { { "apple", 5 } };
            var result = dictionary.GetOrDefault("apple", 10);
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        [Description("Tests that GetOrDefault returns the default value when the key does not exist.")]
        public void GetOrDefault_KeyDoesNotExist_ReturnsDefaultValue()
        {
            var dictionary = new Dictionary<string, int> { { "apple", 5 } };
            var result = dictionary.GetOrDefault("banana", 10);
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        [Description("Tests that CollectionsEqual correctly compares two equal collections.")]
        public void CollectionsEqual_EqualCollections_ReturnsTrue()
        {
            var list1 = new List<int> { 1, 2, 3 };
            var list2 = new List<int> { 1, 2, 3 };
            Assert.IsTrue(list1.CollectionsEqual(list2));
        }

        [TestMethod]
        [Description("Tests that CollectionsEqual correctly compares two unequal collections.")]
        public void CollectionsEqual_UnequalCollections_ReturnsFalse()
        {
            var list1 = new List<int> { 1, 2, 3 };
            var list2 = new List<int> { 3, 2, 1 };
            Assert.IsFalse(list1.CollectionsEqual(list2));
        }

        [TestMethod]
        [Description("Tests that OnSuccess executes the continuation for a successful task.")]
        public async Task OnSuccess_SuccessfulTask_ExecutesContinuation()
        {
            var task = Task.CompletedTask;
            bool continuationExecuted = false;

            await task.OnSuccess(t =>
            {
                continuationExecuted = true;
                return ;
            });

            Assert.IsTrue(continuationExecuted);
        }

        [TestMethod]
        [Description("Tests WhileAsync loops correctly based on the predicate.")]
        public async Task WhileAsync_PredicateControlsLoop()
        {
            int counter = 0;
            Func<Task<bool>> predicate = () => Task.FromResult(counter < 3);
            Func<Task> body = () =>
            {
                counter++;
                return Task.CompletedTask;
            };

            await InternalExtensions.WhileAsync(predicate, body);

            Assert.AreEqual(3, counter);
        }
    

        [TestMethod]
        [Description("Tests that OnSuccess with Action<Task> executes the continuation for a successful task.")]
        public async Task OnSuccess_Action_SuccessfulTask_ExecutesContinuation()
        {
            var task = Task.CompletedTask;
            bool continuationExecuted = false;

            await task.OnSuccess(t =>
            {
                continuationExecuted = true;
            });

            Assert.IsTrue(continuationExecuted);
        }

        [TestMethod]
        [Description("Tests that OnSuccess with Func<Task, Task<T>> executes the continuation for a successful task.")]
        public async Task OnSuccess_Func_SuccessfulTask_ExecutesContinuationAndReturnsResult()
        {
            var task = Task.CompletedTask;
            bool continuationExecuted = false;

            var result = await task.OnSuccess(t =>
            {
                continuationExecuted = true;
                return Task.FromResult("Success");
            });

            Assert.IsTrue(continuationExecuted);
            Assert.AreEqual("Success", result);
        }

        [TestMethod]
        [Description("Tests that OnSuccess propagates a faulted task's exception.")]
        public async Task OnSuccess_FaultedTask_ThrowsException()
        {
            // Arrange: Create a task that is already failed.
            var faultedTask = Task.FromException(new InvalidOperationException("Test Exception"));

            // Act & Assert: We expect that calling OnSuccess on this task will immediately
            // re-throw the original exception.
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                // The continuation is a simple action that would run if the task succeeded.
                // Since the task failed, this action will never be executed.
                faultedTask.OnSuccess(task => { /* This code will not be reached */ })
            );
        }

        [TestMethod]
        [Description("Tests that OnSuccess correctly handles a canceled task.")]
        public async Task OnSuccess_CanceledTask_ReturnsCanceledTask()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();

            var canceledTask = tcs.Task;

            var resultTask = canceledTask.OnSuccess(t => Task.FromResult("should not run"));

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () => await resultTask);
        }

       
    }
}